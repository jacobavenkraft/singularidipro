#nullable enable
using System;
using System.IO;
using MeltySynth;
using Singularidi.Audio;
using Singularidi.Config;
using UnityEngine;

namespace Singularidi.Unity.Audio
{
    // Realtime audio engine: a single MeltySynth Synthesizer instance driven from OnAudioFilterRead.
    //
    // SoundFont discovery order:
    //   1. AppConfig.SoundFontPath if set and the file exists.
    //   2. First *.sf2 (alphabetical) under Application.streamingAssetsPath/SoundFonts/.
    //   3. None found — engine logs both checked locations and stays inert. NoteOn/Off become no-ops.
    //
    // Threading: NoteOn/NoteOff/Load/Play/Pause/Stop are called from the main thread (Unity Update,
    // UI handlers, MidiPlaybackEngine NoteTriggered). Render runs on the audio thread inside
    // OnAudioFilterRead. _synthLock serializes synth state mutations and reads. Allocations are
    // forbidden inside the lock.
    [RequireComponent(typeof(AudioSource))]
    public sealed class MeltySynthAudioEngine : MonoBehaviour, IAudioEngine
    {
        // Scratch buffers allocated up-front so OnAudioFilterRead never allocates.
        // Size derived from AudioSettings.GetConfiguration().dspBufferSize in Awake; the
        // OnAudioFilterRead chunk loop also handles oversized data by looping, so this is
        // a "right-sized common case" optimization rather than a hard upper bound.
        private const int MinBufferSamples = 256;

        private readonly object _synthLock = new object();

        private SoundFont? _soundFont;
        private Synthesizer? _synthesizer;
        private AudioSource? _audioSource;
        private int _sampleRate;
        private int _maxBufferSamples;

        private float[]? _leftBuf;
        private float[]? _rightBuf;

        private bool Ready => _synthesizer != null;

        private void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            var audioConfig = AudioSettings.GetConfiguration();
            _maxBufferSamples = Math.Max(MinBufferSamples, audioConfig.dspBufferSize);
            _leftBuf = new float[_maxBufferSamples];
            _rightBuf = new float[_maxBufferSamples];

            _audioSource = GetComponent<AudioSource>();

            // Load the SoundFont BEFORE starting the audio source. AudioSource.Play() inside
            // ConfigureAudioSourceForFilter can begin invoking OnAudioFilterRead on the audio
            // thread; if the synth isn't constructed yet, that callback clears its buffer
            // (correct but wastes a beat). Reordering eliminates the race entirely.
            TryLoadSoundFont();
            ConfigureAudioSourceForFilter(_audioSource);
        }

        private void OnDestroy() => Dispose();

        public void LoadFile(string midiFilePath)
        {
            // MidiPlaybackEngine on the main thread is the timing authority, so this engine
            // does not parse MIDI itself. We log for diagnostic continuity with the IAudioEngine
            // contract (the legacy engine stored the path; this one doesn't need to).
            if (!File.Exists(midiFilePath))
            {
                Debug.LogWarning($"[MeltySynthAudioEngine] LoadFile called with missing path '{midiFilePath}'.");
                return;
            }
            Debug.Log($"[MeltySynthAudioEngine] LoadFile '{midiFilePath}' (acknowledged; timing driven by MidiPlaybackEngine).");
        }

        public void Play()
        {
            if (!Ready)
            {
                LogInertNoteWarning(nameof(Play));
                return;
            }
            if (_audioSource != null && !_audioSource.isPlaying)
                _audioSource.Play();
        }

        public void Pause()
        {
            if (_audioSource != null && _audioSource.isPlaying)
                _audioSource.Pause();
        }

        public void Stop()
        {
            if (_audioSource != null)
                _audioSource.Stop();

            lock (_synthLock)
            {
                _synthesizer?.Reset();
            }
        }

        public void NoteOn(int channel, int noteNumber, int velocity)
        {
            if (!Ready)
            {
                LogInertNoteWarning(nameof(NoteOn));
                return;
            }
            lock (_synthLock)
            {
                _synthesizer!.NoteOn(channel, noteNumber, velocity);
            }
        }

        public void NoteOff(int channel, int noteNumber)
        {
            if (!Ready) return;
            lock (_synthLock)
            {
                _synthesizer!.NoteOff(channel, noteNumber);
            }
        }

        public void Dispose()
        {
            if (_audioSource != null && _audioSource.isPlaying)
                _audioSource.Stop();

            lock (_synthLock)
            {
                _synthesizer = null;
                _soundFont = null;
            }
        }

        // Called by Unity on the audio thread. data is interleaved L,R,L,R,... for channels==2.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            int totalSamples = data.Length / channels;
            int offset = 0;

            while (offset < totalSamples)
            {
                int chunk = Math.Min(_maxBufferSamples, totalSamples - offset);

                lock (_synthLock)
                {
                    if (_synthesizer == null)
                    {
                        Array.Clear(data, offset * channels, chunk * channels);
                        offset += chunk;
                        continue;
                    }

                    _synthesizer.Render(_leftBuf!.AsSpan(0, chunk), _rightBuf!.AsSpan(0, chunk));
                }

                WriteInterleaved(data, offset * channels, _leftBuf!, _rightBuf!, chunk, channels);
                offset += chunk;
            }
        }

        private static void WriteInterleaved(float[] data, int dataOffset, float[] left, float[] right, int samples, int channels)
        {
            if (channels == 2)
            {
                for (int i = 0; i < samples; i++)
                {
                    data[dataOffset + i * 2]     = left[i];
                    data[dataOffset + i * 2 + 1] = right[i];
                }
                return;
            }

            if (channels == 1)
            {
                for (int i = 0; i < samples; i++)
                    data[dataOffset + i] = 0.5f * (left[i] + right[i]);
                return;
            }

            // >2 channels: write L/R to first two; zero the rest. Rare on desktop.
            for (int i = 0; i < samples; i++)
            {
                int baseIdx = dataOffset + i * channels;
                data[baseIdx]     = left[i];
                data[baseIdx + 1] = right[i];
                for (int c = 2; c < channels; c++)
                    data[baseIdx + c] = 0f;
            }
        }

        private void ConfigureAudioSourceForFilter(AudioSource source)
        {
            // OnAudioFilterRead fires only while the source is playing. A 1-sample silent
            // looping AudioClip is the standard trick to keep the audio thread spinning.
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.loop = true;
            source.bypassEffects = true;
            source.bypassListenerEffects = true;
            source.bypassReverbZones = true;

            if (source.clip == null)
            {
                var silent = AudioClip.Create("Silent", 1, 1, _sampleRate, false);
                silent.SetData(new float[] { 0f }, 0);
                source.clip = silent;
            }
            source.Play();
        }

        private void TryLoadSoundFont()
        {
            var configService = new Singularidi.Unity.Config.UnityConfigService();
            var cfg = configService.Load();

            string? resolved = ResolveSoundFontPath(cfg);
            string streamingDir = Path.Combine(Application.streamingAssetsPath, "SoundFonts");

            if (resolved == null)
            {
                Debug.LogError(
                    "[MeltySynthAudioEngine] No SoundFont found. Audio will be silent.\n" +
                    $"  Checked AppConfig.SoundFontPath: '{cfg.SoundFontPath}'\n" +
                    $"  Checked StreamingAssets folder: '{streamingDir}'\n" +
                    "  See Assets/StreamingAssets/SoundFonts/README.md for setup instructions.");
                return;
            }

            try
            {
                _soundFont = new SoundFont(resolved);
                _synthesizer = new Synthesizer(_soundFont, _sampleRate);
                Debug.Log($"[MeltySynthAudioEngine] Loaded SoundFont '{resolved}' at {_sampleRate} Hz.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MeltySynthAudioEngine] Failed to load SoundFont '{resolved}': {ex.Message}.");
                _soundFont = null;
                _synthesizer = null;
            }
        }

        private static string? ResolveSoundFontPath(AppConfig cfg)
        {
            if (!string.IsNullOrEmpty(cfg.SoundFontPath) && File.Exists(cfg.SoundFontPath))
                return cfg.SoundFontPath;

            string streamingDir = Path.Combine(Application.streamingAssetsPath, "SoundFonts");
            if (!Directory.Exists(streamingDir))
                return null;

            var candidates = Directory.GetFiles(streamingDir, "*.sf2");
            if (candidates.Length == 0)
                return null;

            Array.Sort(candidates, StringComparer.OrdinalIgnoreCase);
            return candidates[0];
        }

        private static void LogInertNoteWarning(string call)
        {
            Debug.LogWarning($"[MeltySynthAudioEngine] {call}() called but the engine is inert (no SoundFont loaded). Add a .sf2 to Assets/StreamingAssets/SoundFonts/ or set AppConfig.SoundFontPath.");
        }
    }
}
