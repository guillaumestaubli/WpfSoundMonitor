using System;
using System.Collections.Generic;
using System.Text;

namespace WpfSoundMonitor.Core
{
    public class AdvancedSample
    {
        private readonly Guid _guid = Guid.NewGuid();

        public Guid GUID { get { return _guid; } }
        public bool IsOpen { get; private set; } = false;
        public int StartIndex { get; private set; } = int.MinValue;
        public int EndIndex { get; private set; } = int.MinValue;
        public List<float> Values { get; private set; } = new List<float>();
        public List<float> AvgValues { get; private set; } = new List<float>();
        public int ValuesCount { get; private set; } = 0;
        public float ValuesSum { get; private set; } = 0.0f;

        /// <summary>
        /// Constructor for the object
        /// </summary>
        /// <param name="startIndex">Index of the first element on the capture device datastream</param>
        private AdvancedSample(int startIndex)
        {
            StartIndex = startIndex;
            EndIndex = startIndex;
            IsOpen = true;
        }

        /// <summary>
        /// Create a new sample in which it is possible to add datas with the AppendValue function
        /// </summary>
        /// <param name="startIndex">Index of the first element on the capture device datastream</param>
        /// <param name="value">Value of the datastream sample</param>
        /// <returns>Open sample</returns>
        public static AdvancedSample OpenSample(int startIndex, float value)
        {
            AdvancedSample sample = new AdvancedSample(startIndex);

            sample.AppendValue(value);
            sample.EndIndex = sample.StartIndex;

            return sample;
        }

        /// <summary>
        /// Append a value from the capture device datatream to the sample
        /// </summary>
        /// <param name="value">Value of the datastream to append</param>
        public void AppendValue(float value)
        {
            // BUG HERE !!!

            ++ValuesCount;
            ValuesSum += value;

            ++EndIndex;

            Values.Add(value);
            AvgValues.Add(ValuesSum / ValuesCount);
        }

        /// <summary>
        /// Close the sample (disable the modification of the values)
        /// </summary>
        /// <param name="endIndex">Index of the last element on the capture device datastream</param>
        /// <param name="value">Value of the datastream sample</param>
        public void Close(int endIndex, float value)
        {
            AppendValue(value);

            EndIndex = endIndex;

            IsOpen = false;
        }
    }
}
