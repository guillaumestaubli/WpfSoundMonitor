using System;
using System.Collections.Generic;
using System.Text;

namespace WpfSoundMonitor
{
    public static class Config
    {
        // Reference values for MainWindow user input
        public const int BASE_MAIN_W_SAMPLE_BUFFER_DURATION_MS = 100;
        public const float BASE_MAIN_W_START_THRESHOLD = 0.5f;
        public const int BASE_MAIN_W_ANALYZE_DURATION_MS = 5000;
        public const float BASE_MAIN_W_MAX_AVERAGE_VALUE = 1.0f;

        
        public const float SOUND_WAVE_DRAW_POINT_X_SPACING = 5.0f;
        public const float SFML_RENDER_AREA_HEIGHT_PX = 200.0f;
        public const float SFML_RENDER_AREA_PADDING_PX = 5.0f;



        public static int MAIN_W_SAMPLE_BUFFER_DURATION_MS { get; set; } = BASE_MAIN_W_SAMPLE_BUFFER_DURATION_MS;
        public static float MAIN_W_START_THRESHOLD { get; set; } = BASE_MAIN_W_START_THRESHOLD;
        public static int MAIN_W_ANALYZE_DURATION_MS { get; set; } = BASE_MAIN_W_ANALYZE_DURATION_MS;
        public static float MAIN_W_MAX_AVERAGE_VALUE { get; set; } = BASE_MAIN_W_MAX_AVERAGE_VALUE;
    }
}
