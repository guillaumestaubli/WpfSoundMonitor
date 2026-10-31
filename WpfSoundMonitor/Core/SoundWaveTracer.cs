using System;
using System.Collections.Generic;
using System.Text;

using SFML;
using SFML.System;
using SFML.Graphics;
using System.Linq;

namespace WpfSoundMonitor.Core
{
    /// <summary>
    /// Implement tools to trace the sound wave captured by a capture device
    /// </summary>
    public static class SoundWaveTracer
    {
        private static VertexArray _vaSoundWave = new VertexArray();
        private static VertexArray _vaAvgWave = new VertexArray();
        private static List<DrawableAdvancedSample> _drawablesSamples = new List<DrawableAdvancedSample>();

        private static List<float> _samplesMaxValues = new List<float>();
        private static List<float> _samplesAvgValues = new List<float>();
        private static int _samplesCount = 0;
        private static float _samplesValuesSum = 0.0f;
        private static float _samplesAbsMaxValue = 0.0f;

        #region GET_SET

        public static int SamplesCount { get { return _samplesCount; } }
        public static float SamplesCurrentValue { get { return _samplesMaxValues.LastOrDefault(); } }
        public static float SamplesMaxValue { get { return _samplesAbsMaxValue; } }
        public static float SamplesCurrentAvgValue { get { return _samplesAvgValues.LastOrDefault(); } }

        #endregion

        /// <summary>
        /// Initialization of the tracer
        /// </summary>
        public static void Init()
        {
            _vaSoundWave.Dispose();
            _vaAvgWave.Dispose();
            _drawablesSamples.ForEach(x => x.Dispose());

            _vaSoundWave = new VertexArray(PrimitiveType.LineStrip);
            _vaAvgWave = new VertexArray(PrimitiveType.LineStrip);
            _drawablesSamples.Clear();

            _samplesMaxValues.Clear();
            _samplesAvgValues.Clear();
            _samplesCount = 0;
            _samplesValuesSum = 0.0f;
            _samplesAbsMaxValue = 0.0f;
        }

        /// <summary>
        /// Fetch the drawables components of the sound wave render area to display for the user in the SFML render area
        /// </summary>
        /// <returns>Array of drawables SFML objects</returns>
        public static IDrawable[] GetDrawables()
        {
            List<IDrawable> outDrawables = new List<IDrawable>
                {
                    _vaSoundWave,
                    _vaAvgWave,
                };

            foreach(DrawableAdvancedSample drawableSample in _drawablesSamples)
            {
                IDrawable[] drawableItems = drawableSample.GetDrawables();

                outDrawables.AddRange(drawableItems);
            }

            return outDrawables.ToArray();
        }

        /// <summary>
        /// Generate the graph for the capture device datastream
        /// </summary>
        /// <param name="allSamplesValues">All the samples recorded by the capture device</param>
        /// <param name="thresholdSamples">Samples that meet the required threshold</param>
        public static void GenerateRender(float[] allSamplesValues, AdvancedSample[] thresholdSamples)
        {
            TraceDatastreamWave(allSamplesValues);

            TraceThresholdSamples(thresholdSamples);
        }

        /// <summary>
        /// Generate the graph for all the samples recorded by the capture device
        /// </summary>
        /// <param name="values">All the samples recorrded by the capture device</param>
        private static void TraceDatastreamWave(float[] values)
        {
            // append the new datas to the graph
            for (uint i = _vaSoundWave.VertexCount; i < values.Length; ++i)
            {
                _samplesMaxValues.Add(values[i]);
                ++_samplesCount;
                _samplesValuesSum += values[i];

                if (values[i] > _samplesAbsMaxValue)
                    _samplesAbsMaxValue = values[i];

                _samplesAvgValues.Add(_samplesValuesSum / _samplesCount);

                AppendLastValueToSoundWaveVertexArray();
                AppendLastValueToAverageWaveVertexArray();
            }

            // fit the graph in the render area
            AdjustDatastreamWaveToRenderArea();
            AdjustAvgDatastreamWaveToRenderArea();
        }

        /// <summary>
        /// Create a vertex for the last value inserted in the samples values list
        /// </summary>
        private static void AppendLastValueToSoundWaveVertexArray()
        {
            float x = (_samplesMaxValues.Count - 1) * Config.SOUND_WAVE_DRAW_POINT_X_SPACING;
            float y = Config.SFML_RENDER_AREA_HEIGHT_PX - (_samplesMaxValues.LastOrDefault() * Config.SFML_RENDER_AREA_HEIGHT_PX);

            Vector2f pos = new Vector2f(x, y);

            Vertex vertex = new Vertex(pos, Config.COLOR_SOUND_WAVE_DATASTREAM);

            _vaSoundWave.Append(vertex);
        }

        /// <summary>
        /// Create a vertex for the last value inserted in the average values list
        /// </summary>
        private static void AppendLastValueToAverageWaveVertexArray()
        {
            float x = (_samplesAvgValues.Count - 1) * Config.SOUND_WAVE_DRAW_POINT_X_SPACING;
            float y = Config.SFML_RENDER_AREA_HEIGHT_PX - (_samplesAvgValues.LastOrDefault() * Config.SFML_RENDER_AREA_HEIGHT_PX);

            Vector2f pos = new Vector2f(x, y);

            Vertex vertex = new Vertex(pos, Config.COLOR_AVG_WAVE_DATASTREAM);

            _vaAvgWave.Append(vertex);
        }

        /// <summary>
        /// Update the Y values of the vertices to fit the graph on the SFML render area when a new max value is detected (prevent the graph from going out of bounds of the render area)
        /// </summary>
        private static void AdjustDatastreamWaveToRenderArea()
        {
            for (int i = 0; i < _samplesMaxValues.Count; ++i)
            {
                float new_y = Config.SFML_RENDER_AREA_HEIGHT_PX - ((_samplesMaxValues[i] * Config.SFML_RENDER_AREA_HEIGHT_PX) / _samplesAbsMaxValue);

                Vertex vertex = _vaSoundWave[(uint)i];
                vertex.Position.Y = new_y;

                _vaSoundWave[(uint)i] = vertex;
            }
        }

        /// <summary>
        /// Update the Y values of the vertices to fit the graph on the SFML render area when a new max value is detected (prevent the graph from going out of bounds of the render area)
        /// </summary>
        private static void AdjustAvgDatastreamWaveToRenderArea()
        {
            for (int i = 0; i < _samplesAvgValues.Count; ++i)
            {
                float new_y = Config.SFML_RENDER_AREA_HEIGHT_PX - ((_samplesAvgValues[i] * Config.SFML_RENDER_AREA_HEIGHT_PX) / _samplesAbsMaxValue);

                Vertex vertex = _vaAvgWave[(uint)i];
                vertex.Position.Y = new_y;

                _vaAvgWave[(uint)i] = vertex;
            }
        }



        private static void TraceThresholdSamples(AdvancedSample[] thresholdSamples)
        {
            foreach (AdvancedSample sample in thresholdSamples)
            {
                DrawableAdvancedSample? drawable = null;

                int d_index = 0;

                for (d_index = 0; d_index < _drawablesSamples.Count; ++d_index)
                {
                    if (_drawablesSamples[d_index].SampleGuid == sample.GUID)
                    {
                        drawable = _drawablesSamples[d_index];
                        break;
                    }
                }


                if (drawable == null)
                {
                    drawable = new DrawableAdvancedSample(sample, _samplesAbsMaxValue);

                    _drawablesSamples.Add(drawable);
                }
                else
                {
                    drawable.UpdateDrawables();

                    _drawablesSamples[d_index] = drawable;
                }
            }
        }
    }
}
