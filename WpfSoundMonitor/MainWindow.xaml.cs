using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

using NAudio.Wave;

using SFML.Graphics;
using SFML.Window;
using SFML.System;
using System;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace WpfSoundMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private View _sfmlView;
        private Vector2f _sfmlViewPosition = new Vector2f(0.0f, 0.0f);
        private Vector2f _sfmlViewSize = new Vector2f(0.0f, 0.0f);
        private RenderWindow _sfmlWindow;
        private System.Windows.Forms.Control _wpfSfmlSurface;

        /// <summary>
        /// Default constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Create WIN32 surface in winforms for integration in WPF
            _wpfSfmlSurface = new System.Windows.Forms.Control();
            SfmlHost.Child = _wpfSfmlSurface;

            // Init SFML window for graphic rendering
            _sfmlWindow = new RenderWindow(_wpfSfmlSurface.Handle);
            _sfmlView = new View();

            // add the handler for the loaded event of the WPF window
            this.Loaded += MainWindow_Loaded;

            // event for refreshing content of the SFML window
            CompositionTarget.Rendering += OnCompositionRendering;

            // event for managing the resize of the SFML render area in the WPF container
            _wpfSfmlSurface.Resize += WPF_SFML_Surface_Resized;
        }

        #region EVENTS

        /// <summary>
        /// Manage the "Window.Loaded" event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            CaptureDeviceManager.LoadCaptureDevicesFromComputer();

            DisplayCaptureDevicesList();
        }

        /// <summary>
        /// Manage the resize of the SFML render container => SFML render window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WPF_SFML_Surface_Resized(object? sender, EventArgs e)
        {
            _sfmlWindow.Size = new Vector2u((uint)_wpfSfmlSurface.Width, (uint)_wpfSfmlSurface.Height);

            _sfmlViewPosition = new Vector2f(0, 0);
            _sfmlViewSize = new Vector2f(_wpfSfmlSurface.Width, _wpfSfmlSurface.Height);

            _sfmlView = new View(new FloatRect(_sfmlViewPosition, _sfmlViewSize));

            _sfmlWindow.SetView(_sfmlView);

            // adjust scroll bar width to accomodate the new windows width
            ScrollBarSfmlRenderArea.Width = _wpfSfmlSurface.Width;
        }

        /// <summary>
        /// SFML render loop synced with WPF render interval
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCompositionRendering(object? sender, EventArgs e)
        {
            if (SoundAnalyzer.IsCapturing)
            {
                SoundWaveTracer.TraceSoundWave(SoundAnalyzer.SamplesAverageMaxValues.ToArray());
                UpdateSfmlRenderAreaScrollbarMinMaxValues(SoundAnalyzer.SamplesAverageMaxValues.Count);
                SfmlRenderArea_ScrollToEnd();

                TxtBox_SamplesCount.Text = SoundWaveTracer.SamplesCount.ToString();
                TxtBox_CurrentSampleValue.Text = SoundWaveTracer.SamplesCurrentValue.ToString();
                TxtBox_SamplesMaxValue.Text = SoundWaveTracer.SamplesMaxValue.ToString();
                TxtBox_SamplesAvgValue.Text = SoundWaveTracer.SamplesCurrentAvgValue.ToString();
            }

            if (_sfmlWindow != null)
            {
                _sfmlWindow.DispatchEvents();

                _sfmlWindow.Clear(new SFML.Graphics.Color(30, 30, 30));

                // Draw the sound wave and other components of the graph in the SFML render area
                IDrawable[] drawables = SoundWaveTracer.GetDrawables(_sfmlView);

                for(int i = 0; i < drawables.Length; ++i)
                {
                    _sfmlWindow.Draw(drawables[i]);
                }

                _sfmlWindow.Display();
            }
        }

        /// <summary>
        /// Get again the devices list from the OS and display them in the list for user selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_RefreshCaptureDevices_Click(object sender, RoutedEventArgs e)
        {
            ClearCaptureDevicesList();

            CaptureDeviceManager.LoadCaptureDevicesFromComputer();

            DisplayCaptureDevicesList();
        }

        /// <summary>
        /// Manage the user selection of the capture device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Lstbox_CaptureDevices_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender == null || sender is not ListBox)
            {
                CaptureDeviceManager.ClearLoadedCaptureDevice();
                return;
            }

            ListBox lstBox = (ListBox)sender;

            if(lstBox.SelectedItem == null || lstBox.SelectedItem is not ListBoxItem)
            {
                string message =
                    $"An error occured at {nameof(MainWindow)}.{nameof(Lstbox_CaptureDevices_SelectionChanged)} " +
                    $"when trying to fetch capture device from listbox selection";

                string caption = $"WpfSoundMonitor - ERROR";

                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            ListBoxItem itemSelected = (ListBoxItem)lstBox.SelectedItem;

            string deviceId = (string)itemSelected.Tag;

            bool result = CaptureDeviceManager.LoadCaptureDeviceById(deviceId);

            Btn_StartCapture.IsEnabled = result;
            Btn_StopCapture.IsEnabled = false;
        }

        /// <summary>
        /// Start the capture of the audio coming from the selected capture device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_StartCapture_Click(object sender, RoutedEventArgs e)
        {
            Btn_StartCapture.IsEnabled = false;
            Btn_StopCapture.IsEnabled = true;
            TxtBox_SampleDurationMs.IsEnabled = false;
            TxtBox_DurationAverageMs.IsEnabled = false;

            float sampleDurationMs = 0.0f;
            float durationAverageMs = 0.0f;

            if (!float.TryParse(TxtBox_SampleDurationMs.Text, out sampleDurationMs))
            {
                string message =
                    $"Failed to parse {TxtBox_SampleDurationMs.Text} from {nameof(TxtBox_SampleDurationMs)} as float " +
                    $"at {nameof(MainWindow)}.{nameof(Btn_StartCapture_Click)}";

                string caption = $"WpfSoundMonitor - ERROR";

                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            if (!float.TryParse(TxtBox_DurationAverageMs.Text, out durationAverageMs))
            {
                string message =
                    $"Failed to parse {TxtBox_DurationAverageMs.Text} from {nameof(TxtBox_DurationAverageMs)} as float " +
                    $"at {nameof(MainWindow)}.{nameof(Btn_StartCapture_Click)}";

                string caption = $"WpfSoundMonitor - ERROR";

                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            if (CaptureDeviceManager.CurrentWasapiRecorder == null)
            {
                string message =
                    $"Capture device not set in {nameof(CaptureDeviceManager)} " +
                    $"at {nameof(MainWindow)}.{nameof(Btn_StartCapture_Click)}";

                string caption = $"WpfSoundMonitor - ERROR";

                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            SoundAnalyzer.Init(CaptureDeviceManager.CurrentWasapiRecorder, sampleDurationMs, durationAverageMs);
            SoundWaveTracer.Init();

            SoundAnalyzer.Start();
        }

        /// <summary>
        /// Stop the capture of the audio coming from the selected capture device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_StopCapture_Click(object sender, RoutedEventArgs e)
        {
            Btn_StartCapture.IsEnabled = true;
            Btn_StopCapture.IsEnabled = false;
            TxtBox_SampleDurationMs.IsEnabled = true;
            TxtBox_DurationAverageMs.IsEnabled = true;

            SoundAnalyzer.Stop();
        }

        /// <summary>
        /// Control the user input for the mandatory number input in textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TxtBox_Number_Integer_NumberValidation(object? sender, TextCompositionEventArgs e)
        {
            string regex_pattern = @"^[\d]*$";

            Regex regex = new Regex(regex_pattern, RegexOptions.None);

            e.Handled = !regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Handle the scroll event on the scrollbar for the sfml render area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollBarSfmlRenderArea_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender == null || sender is not ScrollBar)
                return;

            ScrollBar scrollBar = (ScrollBar)sender;

            float scrollValue_x = (float)scrollBar.Value;

            Vector2f sfmlViewOldPosition = _sfmlViewPosition;
            Vector2f sfmlViewNewPosition = new Vector2f(scrollValue_x, 0.0f);

            Vector2f offset = sfmlViewNewPosition - sfmlViewOldPosition;

            _sfmlView.Move(offset);
            _sfmlWindow.SetView(_sfmlView);
            _sfmlViewPosition += offset;


        }

        #endregion

        /// <summary>
        /// Display the capture devices list on the listbox for user selection
        /// </summary>
        private void DisplayCaptureDevicesList()
        {
            foreach (WasapiRecorder recorder in CaptureDeviceManager.WasapiRecorders)
            {
                ListBoxItem item = new ListBoxItem()
                {
                    Content = recorder.DeviceFriendlyName,  // Used for displaying the name of the capture device
                    Tag = recorder.DeviceId                 // Used for storing the ID of the capture device
                };

                Lstbox_CaptureDevices.Items.Add(item);
            }
        }

        /// <summary>
        /// Delete all items of the listbox
        /// </summary>
        private void ClearCaptureDevicesList()
        {
            Lstbox_CaptureDevices.Items.Clear();
        }

        /// <summary>
        /// Update the scroll bar values depending on the number of sound samples captured.
        /// This makes the user capable of scrolling horizontally to view all the soundwave generated
        /// </summary>
        /// <param name="nbSamples">Number of samples captured</param>
        private void UpdateSfmlRenderAreaScrollbarMinMaxValues(int nbSamples)
        {
            float x_max = nbSamples * Config.SOUND_WAVE_DRAW_POINT_X_SPACING;

            if (x_max <= _sfmlWindow.Size.X)
            {
                ScrollBarSfmlRenderArea.Minimum = 0;
                ScrollBarSfmlRenderArea.Maximum = 0;
            }
            else
            {
                ScrollBarSfmlRenderArea.Minimum = 0;
                ScrollBarSfmlRenderArea.Maximum = x_max - _wpfSfmlSurface.Width;
            }
        }

        /// <summary>
        /// Manually scroll to the end of the graph (used when new datas are displayed on the sfml render area)
        /// </summary>
        private void SfmlRenderArea_ScrollToEnd()
        {
            ScrollBarSfmlRenderArea.Value = ScrollBarSfmlRenderArea.Maximum;
        }
    }
}