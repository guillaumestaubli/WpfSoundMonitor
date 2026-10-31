using System;
using System.Collections.Generic;
using System.Text;

using SFML;
using SFML.System;
using SFML.Graphics;
using System.Linq;

namespace WpfSoundMonitor
{
    /// <summary>
    /// Implement tools to trace the sound wave captured by a capture device
    /// </summary>
    public static class SoundWaveTracer
    {
        private static Color _vaSoundWave_Color = Color.Green;
        private static Color _vaAvgWave_Color = Color.Yellow;

        private static VertexArray _vaSoundWave = new VertexArray();
        private static VertexArray _vaAvgWave = new VertexArray();

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
            _vaSoundWave = new VertexArray(PrimitiveType.LineStrip);
            _vaAvgWave = new VertexArray(PrimitiveType.LineStrip);

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
        public static IDrawable[] GetDrawables(View currentView)
        {
            IDrawable[] drawables =
                {
                    _vaSoundWave,
                    _vaAvgWave
                };

            return drawables;
        }

        /// <summary>
        /// Generate the graph for the values
        /// </summary>
        /// <param name="values"></param>
        public static void TraceSoundWave(float[] values)
        {
            float old_maxValue = _samplesAbsMaxValue;

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

            if (old_maxValue >= _samplesAbsMaxValue)
            {
                UpdateSoundWaveVertexArrayForNewMaxValue();
                UpdateAvgWaveVertexArrayForNewMaxValue();
            }
        }

        /// <summary>
        /// Create a vertex for the last value inserted in the samples values list
        /// </summary>
        private static void AppendLastValueToSoundWaveVertexArray()
        {
            float x = (_samplesMaxValues.Count - 1) * Config.SOUND_WAVE_DRAW_POINT_X_SPACING;
            float y = Config.SFML_RENDER_AREA_HEIGHT_PX - (_samplesMaxValues.LastOrDefault() * Config.SFML_RENDER_AREA_HEIGHT_PX);

            Vector2f pos = new Vector2f(x, y);

            Vertex vertex = new Vertex(pos, _vaSoundWave_Color);

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

            Vertex vertex = new Vertex(pos, _vaAvgWave_Color);

            _vaAvgWave.Append(vertex);
        }

        /// <summary>
        /// Update the Y values of the vertices to fit the graph on the SFML render area when a new max value is detected (prevent the graph from going out of bounds of the render area)
        /// </summary>
        private static void UpdateSoundWaveVertexArrayForNewMaxValue()
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
        private static void UpdateAvgWaveVertexArrayForNewMaxValue()
        {
            for (int i = 0; i < _samplesAvgValues.Count; ++i)
            {
                float new_y = Config.SFML_RENDER_AREA_HEIGHT_PX - ((_samplesAvgValues[i] * Config.SFML_RENDER_AREA_HEIGHT_PX) / _samplesAbsMaxValue);

                Vertex vertex = _vaAvgWave[(uint)i];
                vertex.Position.Y = new_y;

                _vaAvgWave[(uint)i] = vertex;
            }
        }
    }
}
