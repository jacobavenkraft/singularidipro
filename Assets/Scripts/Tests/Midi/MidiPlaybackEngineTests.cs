using System.Collections.Generic;
using NUnit.Framework;
using Singularidi.Midi;
using Singularidi.Time;

namespace Singularidi.Tests.Midi
{
    public class MidiPlaybackEngineTests
    {
        private sealed class FakeClock : IPlaybackClock
        {
            public double NowSeconds { get; set; }
            public bool IsRunning { get; private set; }

            public void Start() => IsRunning = true;
            public void Pause() => IsRunning = false;
            public void Reset() { NowSeconds = 0; IsRunning = false; }

            public void Advance(double seconds)
            {
                if (IsRunning) NowSeconds += seconds;
            }
        }

        private sealed class FakeParser : IMidiFileParser
        {
            public List<NoteEvent> Notes { get; set; } = new List<NoteEvent>();
            public double Duration { get; set; }

            public (List<NoteEvent> Notes, double TotalDurationSeconds) Parse(string path)
                => (Notes, Duration);
        }

        private static (MidiPlaybackEngine engine, FakeClock clock) BuildEngine(List<NoteEvent> notes, double duration)
        {
            var clock = new FakeClock();
            var parser = new FakeParser { Notes = notes, Duration = duration };
            var engine = new MidiPlaybackEngine(clock, parser);
            return (engine, clock);
        }

        [Test]
        public void Load_TransitionsToLoaded()
        {
            var (engine, _) = BuildEngine(new List<NoteEvent>(), 1.0);
            engine.Load("dummy");
            Assert.That(engine.State, Is.EqualTo(PlaybackState.Loaded));
        }

        [Test]
        public void Play_TransitionsToPlaying_AndStartsClock()
        {
            var (engine, clock) = BuildEngine(new List<NoteEvent>(), 1.0);
            engine.Load("dummy");
            engine.Play();
            clock.Advance(0.25);
            Assert.That(engine.State, Is.EqualTo(PlaybackState.Playing));
            Assert.That(engine.CurrentTime, Is.EqualTo(0.25).Within(1e-9));
        }

        [Test]
        public void NoteTriggered_FiresAtExpectedTimes()
        {
            var notes = new List<NoteEvent>
            {
                new NoteEvent(60, 0, 100, 0.0, 1.0, 0),
                new NoteEvent(62, 0, 100, 0.5, 1.5, 0),
                new NoteEvent(64, 0, 100, 1.0, 2.0, 0),
            };
            var (engine, clock) = BuildEngine(notes, 2.0);
            var fired = new List<NoteEvent>();
            engine.NoteTriggered += n => fired.Add(n);

            engine.Load("dummy");
            engine.Play();

            engine.UpdateNoteEvents();
            Assert.That(fired.Count, Is.EqualTo(1), "Note 0 has start=0.0 and should fire on the first Update.");

            clock.Advance(0.4);
            engine.UpdateNoteEvents();
            Assert.That(fired.Count, Is.EqualTo(1), "At t=0.4, note 1 (start=0.5) hasn't started yet.");

            clock.Advance(0.1);
            engine.UpdateNoteEvents();
            Assert.That(fired.Count, Is.EqualTo(2), "At t=0.5, note 1 should fire.");

            clock.Advance(0.5);
            engine.UpdateNoteEvents();
            Assert.That(fired.Count, Is.EqualTo(3), "At t=1.0, note 2 should fire.");
        }

        [Test]
        public void Pause_PreservesElapsedTime()
        {
            var (engine, clock) = BuildEngine(new List<NoteEvent>(), 5.0);
            engine.Load("dummy");
            engine.Play();
            clock.Advance(0.7);
            engine.Pause();
            Assert.That(engine.State, Is.EqualTo(PlaybackState.Paused));

            clock.Advance(2.0); // clock paused, advance ignored
            Assert.That(engine.CurrentTime, Is.EqualTo(0.7).Within(1e-9));

            engine.Play();
            clock.Advance(0.3);
            Assert.That(engine.CurrentTime, Is.EqualTo(1.0).Within(1e-9));
        }

        [Test]
        public void Stop_ResetsElapsedAndNoteIndex()
        {
            var notes = new List<NoteEvent>
            {
                new NoteEvent(60, 0, 100, 0.0, 1.0, 0),
                new NoteEvent(62, 0, 100, 0.5, 1.5, 0),
            };
            var (engine, clock) = BuildEngine(notes, 2.0);
            var fired = new List<NoteEvent>();
            engine.NoteTriggered += n => fired.Add(n);

            engine.Load("dummy");
            engine.Play();
            clock.Advance(0.6);
            engine.UpdateNoteEvents();
            Assert.That(fired.Count, Is.EqualTo(2));

            engine.Stop();
            Assert.That(engine.State, Is.EqualTo(PlaybackState.Loaded));
            Assert.That(engine.CurrentTime, Is.EqualTo(0).Within(1e-9));

            // After Stop+Play, notes from t=0 should fire again.
            engine.Play();
            engine.UpdateNoteEvents();
            Assert.That(fired.Count, Is.EqualTo(3), "Note 0 should fire again after Stop+Play.");
        }

        [Test]
        public void UpdateNoteEvents_TransitionsToFinishedAtEndOfFile()
        {
            var (engine, clock) = BuildEngine(new List<NoteEvent>(), 1.0);
            engine.Load("dummy");
            engine.Play();
            clock.Advance(1.5);
            engine.UpdateNoteEvents();
            Assert.That(engine.State, Is.EqualTo(PlaybackState.Finished));
        }
    }
}
