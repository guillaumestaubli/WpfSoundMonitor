using System;
using System.Collections.Generic;
using System.Text;

using SFML;
using SFML.System;
using SFML.Graphics;

namespace WpfSoundMonitor
{
    public static class SoundWaveTracer
    {
        public static IDrawable? DrawableSoundWave { get { return _vaSoundWave; } }

        private static VertexArray _vaSoundWave = new VertexArray();

        public static void Init()
        {
            _vaSoundWave = new VertexArray(PrimitiveType.LineStrip);
        }

        /// <summary>
        /// Append the values to the trace and generate the visual render
        /// </summary>
        /// <param name="values"></param>
        public static void TraceSoundWave(float[] values)
        {
            for (uint i = _vaSoundWave.VertexCount; i < values.Length; ++i)
            {
                float x = i * 5;
                float y = 200.0f - (values[i] * 200);

                Vector2f pos = new Vector2f(x, y);
                Color color = Color.Green;

                Vertex vertex = new Vertex(pos, color);

                _vaSoundWave.Append(vertex);
            }
        }
    }
}
