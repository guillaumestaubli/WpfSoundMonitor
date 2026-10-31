using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace WpfSoundMonitor.Core
{
    public class DrawableAdvancedSample
    {
        public Guid SampleGuid { get { return _sample.GUID; } }

        private AdvancedSample _sample;
        private float _maxValue_Y;

        private VertexArray _vaBackground = new VertexArray();
        private VertexArray _vaAverageWave = new VertexArray();

        public DrawableAdvancedSample(AdvancedSample sample, float maxValue_Y)
        {
            _sample = sample;
            _maxValue_Y = maxValue_Y;

            InitializeBackground();
        }

        public void Dispose()
        {
            _vaBackground.Dispose();
            _vaAverageWave.Dispose();
        }

        public void UpdateDrawables()
        {
            UpdateBackground();
            UpdateWave();
        }

        /// <summary>
        /// Return drawable components of the sample to display on the SFML render area
        /// </summary>
        /// <returns>Colletion of SFML drawable objects</returns>
        public IDrawable[] GetDrawables()
        {
            IDrawable[] outDrawables =
            {
                _vaBackground,
                _vaAverageWave
            };

            return outDrawables;
        }

        /// <summary>
        /// Create the background of the sample to display on the graph (TriangleStrip, 4 vertices)
        /// </summary>
        private void InitializeBackground()
        {
            _vaBackground = new VertexArray(PrimitiveType.TriangleStrip, 4);

            Vertex topLeft = new Vertex();
            Vertex bottomLeft = new Vertex();
            Vertex topRight = new Vertex();
            Vertex bottomRight = new Vertex();

            topLeft.Color = Config.COLOR_SAMPLE_BACKGROUND;
            bottomLeft.Color = Config.COLOR_SAMPLE_BACKGROUND;
            topRight.Color = Config.COLOR_SAMPLE_BACKGROUND;
            bottomRight.Color = Config.COLOR_SAMPLE_BACKGROUND;

            topLeft.Position = new Vector2f(
                x: _sample.StartIndex * Config.SOUND_WAVE_DRAW_POINT_X_SPACING,
                y: 0.0f);

            bottomLeft.Position = new Vector2f(
                x: _sample.StartIndex * Config.SOUND_WAVE_DRAW_POINT_X_SPACING,
                y: Config.SFML_RENDER_AREA_HEIGHT_PX);

            topRight.Position = new Vector2f(
                x: _sample.EndIndex * Config.SOUND_WAVE_DRAW_POINT_X_SPACING,
                y: 0.0f);

            bottomRight.Position = new Vector2f(
                x: _sample.EndIndex * Config.SOUND_WAVE_DRAW_POINT_X_SPACING,
                y: Config.SFML_RENDER_AREA_HEIGHT_PX);

            _vaBackground[0] = topLeft;
            _vaBackground[1] = bottomLeft;
            _vaBackground[2] = topRight;
            _vaBackground[3] = bottomRight;
        }

        /// <summary>
        /// Update the background of the sample to display on the graph when new datas are added in the sample
        /// </summary>
        private void UpdateBackground()
        {
            Vertex topLeft = _vaBackground[0];
            Vertex bottomLeft = _vaBackground[1];
            Vertex topRight = _vaBackground[2];
            Vertex bottomRight = _vaBackground[3];

            topLeft.Position = new Vector2f(
                x: _sample.StartIndex * Config.SOUND_WAVE_DRAW_POINT_X_SPACING,
                y: 0.0f);

            bottomLeft.Position = new Vector2f(
                x: _sample.StartIndex * Config.SOUND_WAVE_DRAW_POINT_X_SPACING,
                y: Config.SFML_RENDER_AREA_HEIGHT_PX);

            topRight.Position = new Vector2f(
                x: _sample.EndIndex * Config.SOUND_WAVE_DRAW_POINT_X_SPACING,
                y: 0.0f);

            bottomRight.Position = new Vector2f(
                x: _sample.EndIndex * Config.SOUND_WAVE_DRAW_POINT_X_SPACING,
                y: Config.SFML_RENDER_AREA_HEIGHT_PX);

            _vaBackground[0] = topLeft;
            _vaBackground[1] = bottomLeft;
            _vaBackground[2] = topRight;
            _vaBackground[3] = bottomRight;
        }

        private void InitializeWave()
        {
            _vaAverageWave = new VertexArray(PrimitiveType.LineStrip);

            Vertex firstPoint = new Vertex();
            firstPoint.Color = Config.COLOR_SAMPLE_AVG_WAVE;

            firstPoint.Position = new Vector2f(
                x: 0.0f,
                y: 0.0f);
        }

        private void UpdateWave()
        {
            _vaAverageWave.Dispose();
            _vaAverageWave = new VertexArray(PrimitiveType.LineStrip);

            for(int i = _sample.StartIndex; i < _sample.EndIndex; ++i)
            {
                float value = SoundAnalyzer.SamplesAverageMaxValues[i];

                float x = i * Config.SOUND_WAVE_DRAW_POINT_X_SPACING;
                float y = Config.SFML_RENDER_AREA_HEIGHT_PX - (_sample.AvgValues[i - _sample.StartIndex] * Config.SFML_RENDER_AREA_HEIGHT_PX);

                Vector2f pos = new Vector2f(x, y);

                Vertex vertex = new Vertex(pos, Config.COLOR_SAMPLE_AVG_WAVE);

                _vaAverageWave.Append(vertex);
            }
        }



    }
}
