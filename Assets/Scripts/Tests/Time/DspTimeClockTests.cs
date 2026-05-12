#nullable enable
using NUnit.Framework;
using Singularidi.Unity.Time;

namespace Singularidi.Tests.Time
{
    public class DspTimeClockTests
    {
        // Driver lets the test simulate AudioSettings.dspTime advancing without a running audio system.
        private sealed class FakeDsp
        {
            public double Now;
            public double Read() => Now;
        }

        [Test]
        public void NowSeconds_BeforeStart_IsZero()
        {
            var dsp = new FakeDsp { Now = 5.0 };
            var clock = new DspTimeClock(dsp.Read);

            Assert.That(clock.NowSeconds, Is.EqualTo(0.0).Within(1e-9));
        }

        [Test]
        public void Start_Then_Advance_NowSecondsReflectsDelta()
        {
            var dsp = new FakeDsp { Now = 100.0 };
            var clock = new DspTimeClock(dsp.Read);

            clock.Start();
            dsp.Now = 102.5;

            Assert.That(clock.NowSeconds, Is.EqualTo(2.5).Within(1e-9));
        }

        [Test]
        public void Pause_FreezesNowSeconds()
        {
            var dsp = new FakeDsp { Now = 100.0 };
            var clock = new DspTimeClock(dsp.Read);

            clock.Start();
            dsp.Now = 101.0;
            clock.Pause();
            dsp.Now = 110.0; // dsp time still advancing in the real world

            Assert.That(clock.NowSeconds, Is.EqualTo(1.0).Within(1e-9));
        }

        [Test]
        public void Start_AfterPause_ResumesFromAccumulatedTime()
        {
            var dsp = new FakeDsp { Now = 0.0 };
            var clock = new DspTimeClock(dsp.Read);

            clock.Start();
            dsp.Now = 2.0;
            clock.Pause();      // accumulated = 2.0
            dsp.Now = 50.0;     // gap while paused
            clock.Start();      // resume from accumulated
            dsp.Now = 53.5;

            Assert.That(clock.NowSeconds, Is.EqualTo(5.5).Within(1e-9), "Expected 2.0 (pre-pause) + 3.5 (since resume).");
        }

        [Test]
        public void Reset_ZeroesAccumulatedTimeAndStops()
        {
            var dsp = new FakeDsp { Now = 10.0 };
            var clock = new DspTimeClock(dsp.Read);

            clock.Start();
            dsp.Now = 15.0;
            clock.Reset();

            Assert.That(clock.NowSeconds, Is.EqualTo(0.0).Within(1e-9));

            dsp.Now = 20.0;
            Assert.That(clock.NowSeconds, Is.EqualTo(0.0).Within(1e-9), "Reset must also stop ticking.");
        }

        [Test]
        public void Start_WhenAlreadyRunning_Idempotent()
        {
            var dsp = new FakeDsp { Now = 0.0 };
            var clock = new DspTimeClock(dsp.Read);

            clock.Start();
            dsp.Now = 1.0;
            clock.Start(); // second Start must not reset the anchor
            dsp.Now = 2.0;

            Assert.That(clock.NowSeconds, Is.EqualTo(2.0).Within(1e-9));
        }

        [Test]
        public void Pause_WhenAlreadyPaused_Idempotent()
        {
            var dsp = new FakeDsp { Now = 0.0 };
            var clock = new DspTimeClock(dsp.Read);

            clock.Start();
            dsp.Now = 3.0;
            clock.Pause();
            dsp.Now = 99.0;
            clock.Pause(); // second Pause must not re-accumulate

            Assert.That(clock.NowSeconds, Is.EqualTo(3.0).Within(1e-9));
        }

        [Test]
        public void NowSeconds_IsMonotonicallyNonDecreasing_AcrossRunPauseCycles()
        {
            var dsp = new FakeDsp { Now = 0.0 };
            var clock = new DspTimeClock(dsp.Read);

            double[] samples = new double[6];
            clock.Start();
            dsp.Now = 1.0; samples[0] = clock.NowSeconds;
            dsp.Now = 2.0; samples[1] = clock.NowSeconds;
            clock.Pause();
            dsp.Now = 100.0; samples[2] = clock.NowSeconds;
            clock.Start();
            dsp.Now = 100.5; samples[3] = clock.NowSeconds;
            dsp.Now = 101.0; samples[4] = clock.NowSeconds;
            clock.Pause();
            dsp.Now = 200.0; samples[5] = clock.NowSeconds;

            for (int i = 1; i < samples.Length; i++)
                Assert.That(samples[i], Is.GreaterThanOrEqualTo(samples[i - 1]), $"Sample {i} ({samples[i]}) < sample {i - 1} ({samples[i - 1]}).");
        }
    }
}
