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
        public static float SampleDurationMs { get; set; } = 0.0f;
        public static List<float> SamplesAverageMaxValues { get; private set; } = new List<float>();

        private static float SampleAverageMaxValue_Count = 0.0f;
        private static float SampleAverageMaxValue_Value = 0.0f;

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
        /// Start capture of the sound and analyze it
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

                SampleAverageMaxValue_Value += bufferMaxValue;
                ++SampleAverageMaxValue_Count;
            }

            // calculate the average value of the sample
            if (_clock_sample.ElapsedMilliseconds >= SampleDurationMs)
            {
                _clock_sample.Restart();

                float sampleAverage = 0.0f;

                if (SampleAverageMaxValue_Count != 0)
                    sampleAverage = SampleAverageMaxValue_Value / SampleAverageMaxValue_Count;

                SamplesAverageMaxValues.Add(sampleAverage);

                SampleAverageMaxValue_Value = 0.0f;
                SampleAverageMaxValue_Count = 0.0f;


            }
        }
    }
}
