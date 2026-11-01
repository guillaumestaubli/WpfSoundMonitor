using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfSoundMonitor.Windows
{
    /// <summary>
    /// Logique d'interaction pour TooMuchNoiseWindow.xaml
    /// </summary>
    public partial class TooMuchNoiseWindow : Window
    {
        public TooMuchNoiseWindow()
        {
            InitializeComponent();

            this.Loaded += TooMuchNoiseWindow_Loaded;
            this.KeyUp += TooMuchNoiseWindow_KeyUp;
        }

        #region EVENTS

        private void TooMuchNoiseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;

            List<System.Windows.Forms.Screen> allScreens = System.Windows.Forms.Screen.AllScreens.ToList();

            System.Windows.Forms.Screen? screen = allScreens.FirstOrDefault(x => x.Primary == false);

            if (screen == null)
                screen = allScreens.First();

            this.Left = screen.WorkingArea.Left + 10;
            this.Top = screen.WorkingArea.Top + 10;

            this.WindowState = WindowState.Maximized;
        }

        private void TooMuchNoiseWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Hide();
        }

        #endregion
    }
}
