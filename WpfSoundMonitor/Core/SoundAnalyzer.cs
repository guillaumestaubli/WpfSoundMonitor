using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace WpfSoundMonitor.Core
{
    /// <summary>
    /// Implements tools to analyze the sound captured by a WasapiRecorder
    /// </summary>
    public static class SoundAnalyzer
    {
        public static bool IsCapturing { get; private set; } = false;
        public static WasapiRecorder? CaptureDevice { get; private set; } = null;
        public static float SampleDurationMs { get; private set; } = 0.0f;
        public static List<float> SamplesAverageMaxValues { get; private set; } = new List<float>();
        public static List<AdvancedSample> ThresholdSamples { get; private set; } = new List<AdvancedSample>();
        private static AdvancedSample? _currentThresholdSample = null;

        private static int _sampleAverageMaxValue_Count = 0;
        private static float _sampleAverageMaxValue_Value = 0.0f;

        private static Stopwatch _clock_sample = new Stopwatch();
        private static Stopwatch _clock_average = new Stopwatch();


        /// <summary>
        /// Initialization point for the node
        /// Must start the capture process quickly because the internal clock is started here
        /// </summary>
        /// <param name="sampleDurationMs"></param>
        /// <param name="durationAverageMs"></param>
        public static void Init(WasapiRecorder captureDevice, float sampleDurationMs)
        {
            SampleDurationMs = sampleDurationMs;
            SamplesAverageMaxValues.Clear();
            _clock_sample = new Stopwatch();
            _clock_average = new Stopwatch();
            CaptureDevice = captureDevice;
            CaptureDevice.DataAvailable += OnDataAvailable;
        }

        /// <summary>
        /// Start capturing the datastream coming from the selected capture device
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void Start()
        {
            if (CaptureDevice == null)
                throw new Exception($"Capture device not set");

            IsCapturing = true;
            CaptureDevice.StartRecording();
            _clock_sample.Start();
            _clock_average.Start();
        }

        /// <summary>
        /// Stop capturing the datastream coming from the selected capture device
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void Stop()
        {
            if (CaptureDevice == null)
                throw new Exception("Capture device not set");

            IsCapturing = false;
            CaptureDevice.StopRecording();
            _clock_sample.Stop();
            _clock_average.Stop();
        }

        public static void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePos, long qpcPos)
        {
            // this function is called asynchronously from the main thread meaning that in certain cases
            // this event can be trigerred after the stop process of the sound capture
            if (!IsCapturing)
                return;

            float bufferMaxValue = 0.0f;

            // transform the bytes of the buffer into readable float values
            ReadOnlySpan<float> samples = MemoryMarshal.Cast<byte, float>(buffer);

            foreach (float sample in samples)
            {
                float abs = Math.Abs(sample);

                if (abs > bufferMaxValue)
                    bufferMaxValue = abs;

                _sampleAverageMaxValue_Value += bufferMaxValue;
                ++_sampleAverageMaxValue_Count;
            }

            // calculate the average value of the sample
            if (_clock_sample.ElapsedMilliseconds >= SampleDurationMs)
            {
                _clock_sample.Restart();

                float sampleAverage = 0.0f;

                if (_sampleAverageMaxValue_Count != 0)
                    sampleAverage = _sampleAverageMaxValue_Value / _sampleAverageMaxValue_Count;

                SamplesAverageMaxValues.Add(sampleAverage);

                if(_currentThresholdSample == null && sampleAverage >= Config.MAIN_W_START_THRESHOLD)
                {
                    _clock_average.Start();
                    _currentThresholdSample = AdvancedSample.OpenSample(SamplesAverageMaxValues.Count - 1, sampleAverage);
                    ThresholdSamples.Add(_currentThresholdSample);
                }
                else if(_currentThresholdSample != null)
                {
                    if(_clock_average.ElapsedMilliseconds < Config.MAIN_W_ANALYZE_DURATION_MS)
                    {
                        _currentThresholdSample.AppendValue(sampleAverage);

                        ThresholdSamples[ThresholdSamples.Count - 1] = _currentThresholdSample;
                    }
                    else // _clock_average.ElapsedMilliseconds >= Config.MAIN_W_ANALYZE_DURATION_MS
                    {
                        _currentThresholdSample.Close(SamplesAverageMaxValues.Count - 1, sampleAverage);

                        ThresholdSamples[ThresholdSamples.Count - 1] = _currentThresholdSample;

                        _currentThresholdSample = null;

                        _clock_average.Reset();
                    }
                }

                _sampleAverageMaxValue_Value = 0.0f;
                _sampleAverageMaxValue_Count = 0;
            }
        }
    }
}
