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
        public static List<float> DatastreamSamplesValues { get; private set; } = new List<float>();
        public static List<AdvancedSample> ThresholdSamples { get; private set; } = new List<AdvancedSample>();
        public static float DatastreamSamplesMaxValue { get; private set; } = 0.0f;

        private static int _currentDatastreamSample_Count = 0;
        private static float _currentDatastreamSample_Sum = 0.0f;
        private static AdvancedSample? _currentThresholdSample = null;
        private static Stopwatch _cloclDatastreamSample = new Stopwatch();
        private static Stopwatch _clockThresholdSample = new Stopwatch();


        /// <summary>
        /// Initialization point for the node
        /// Must start the capture process quickly because the internal clock is started here
        /// </summary>
        /// <param name="sampleDurationMs"></param>
        /// <param name="sampleDurationMs"></param>
        public static void Init(WasapiRecorder captureDevice, float sampleDurationMs)
        {
            SampleDurationMs = sampleDurationMs;
            DatastreamSamplesMaxValue = Config.MAIN_W_MAX_AVERAGE_VALUE + 0.05f;
            DatastreamSamplesValues.Clear();
            ThresholdSamples.Clear();
            _cloclDatastreamSample = new Stopwatch();
            _clockThresholdSample = new Stopwatch();
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
            _cloclDatastreamSample.Start();
            _clockThresholdSample.Start();
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
            _cloclDatastreamSample.Stop();
            _clockThresholdSample.Stop();
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

                _currentDatastreamSample_Sum += bufferMaxValue;
                ++_currentDatastreamSample_Count;
            }

            // calculate the average value of the sample
            if (_cloclDatastreamSample.ElapsedMilliseconds >= SampleDurationMs)
            {
                _cloclDatastreamSample.Restart();

                float sampleAverage = 0.0f;

                if (_currentDatastreamSample_Count != 0)
                    sampleAverage = _currentDatastreamSample_Sum / _currentDatastreamSample_Count;

                DatastreamSamplesValues.Add(sampleAverage);

                // check if the current sample value is the new maximum value of the datastream
                if (sampleAverage > DatastreamSamplesMaxValue)
                    DatastreamSamplesMaxValue = sampleAverage;

                // check if the sound level reached the threshold to start the capture of a custom sample
                if(_currentThresholdSample == null && sampleAverage >= Config.MAIN_W_START_THRESHOLD)
                {
                    // the threshold is reached and no sample is currently rercording => start a new sample
                    _clockThresholdSample.Restart();
                    _currentThresholdSample = AdvancedSample.OpenSample(DatastreamSamplesValues.Count - 1, sampleAverage);
                    ThresholdSamples.Add(_currentThresholdSample);
                }
                else if(_currentThresholdSample != null)
                {
                    if(_clockThresholdSample.ElapsedMilliseconds < Config.MAIN_W_ANALYZE_DURATION_MS)
                    {
                        // the record time is not yet reached => append the datastream value to the currently running sample
                        _currentThresholdSample.AppendValue(sampleAverage);

                        ThresholdSamples[ThresholdSamples.Count - 1] = _currentThresholdSample;
                    }
                    else // _clockThresholdSample.ElapsedMilliseconds >= Config.MAIN_W_ANALYZE_DURATION_MS
                    {
                        // the record time is reached => append the last sample value and close the sample
                        _currentThresholdSample.Close(DatastreamSamplesValues.Count - 1, sampleAverage);

                        ThresholdSamples[ThresholdSamples.Count - 1] = _currentThresholdSample;

                        _currentThresholdSample = null;

                        _clockThresholdSample.Reset();
                    }
                }

                _currentDatastreamSample_Sum = 0.0f;
                _currentDatastreamSample_Count = 0;
            }
        }

        public static AdvancedSample? GetThresholdSampleAtDatastreamIndex(int index)
        {
            AdvancedSample? outSample = null;

            foreach(AdvancedSample sample in ThresholdSamples)
            {
                if (sample.StartIndex <= index && index <= sample.EndIndex)
                    outSample = sample;
            }

            return outSample;
        }
    }
}
