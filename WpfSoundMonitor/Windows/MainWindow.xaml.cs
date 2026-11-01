using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Globalization;

using NAudio.Wave;

using SFML.System;
using SFML.Graphics;

using WpfSoundMonitor.Core;
using WpfSoundMonitor.Logs;

namespace WpfSoundMonitor.Windows
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
                SoundWaveTracer.GenerateRender(SoundAnalyzer.DatastreamSamplesValues.ToArray(), SoundAnalyzer.ThresholdSamples.ToArray());

                UpdateSfmlRenderAreaScrollbarMinMaxValues(SoundAnalyzer.DatastreamSamplesValues.Count);
                SfmlRenderArea_ScrollToEnd();

                TxtBox_SamplesCount.Text = SoundWaveTracer.SamplesCount.ToString();
                TxtBox_CurrentSampleValue.Text = SoundWaveTracer.SamplesCurrentValue.ToString();
                TxtBox_SamplesMaxValue.Text = SoundAnalyzer.DatastreamSamplesMaxValue.ToString();
                TxtBox_SamplesAvgValue.Text = SoundWaveTracer.SamplesCurrentAvgValue.ToString();
            }

            if (_sfmlWindow != null)
            {
                _sfmlWindow.DispatchEvents();

                _sfmlWindow.Clear(new SFML.Graphics.Color(30, 30, 30));

                // Draw the sound wave and other components of the graph in the SFML render area
                IDrawable[] drawables = SoundWaveTracer.GetDrawables();

                for (int i = 0; i < drawables.Length; ++i)
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

            if (lstBox.SelectedItem == null || lstBox.SelectedItem is not ListBoxItem)
            {
                Logger.LogError("MainWindow.Lstbox_CaptureDevices_SelectionChanged", "An error occured when trying to fetch capture device from listbox selection");

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
            if (CaptureDeviceManager.CurrentWasapiRecorder == null)
            {
                Logger.LogError("MainWindow.Btn_StartCapture_Click", $"Capture device not set in {nameof(CaptureDeviceManager)}");

                string message =
                    $"Capture device not set in {nameof(CaptureDeviceManager)} " +
                    $"at {nameof(MainWindow)}.{nameof(Btn_StartCapture_Click)}";

                string caption = $"WpfSoundMonitor - ERROR";

                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            // Fetch the datas from the user input and check if any errors occured during the process
            if (!StartCapture_LoadUserInputValues())
            {
                Logger.LogError("MainWindow.Btn_StartCapture_Click", $"Failed to load user inputs");

                string message =
                    $"An error occured during {nameof(MainWindow)}.{nameof(StartCapture_LoadUserInputValues)}. " +
                    $"Impossible to load user inputs";

                string caption = $"WpfSoundMonitor - ERROR";

                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            Btn_StartCapture.IsEnabled = false;
            Btn_StopCapture.IsEnabled = true;
            TxtBox_SampleDurationMs.IsEnabled = false;
            TxtBox_StartThreshold.IsEnabled = false;
            TxtBox_AnalyzeDuration.IsEnabled = false;
            TxtBox_MaxAverageValue.IsEnabled = false;

            SoundAnalyzer.Init(CaptureDeviceManager.CurrentWasapiRecorder, Config.MAIN_W_SAMPLE_BUFFER_DURATION_MS);
            SoundWaveTracer.Init();

            SoundAnalyzer.Start();
        }

        private bool StartCapture_LoadUserInputValues()
        {
            bool success = true;

            int sampleBufferDurationMs = 0;
            float startThreshold = 0.0f;
            int analyzeDurationMs = 0;
            float maxAverageValue = 0.0f;

            if (!int.TryParse(TxtBox_SampleDurationMs.Text, CultureInfo.InvariantCulture, out sampleBufferDurationMs))
            {
                Logger.LogWarn("MainWindow.StartCapture_LoadUserInputValues", $"Can't parse TxtBox_SampleDurationMs.Text = {TxtBox_SampleDurationMs.Text} as int");
                 
                success = false;
            }

            if (!float.TryParse(TxtBox_StartThreshold.Text, CultureInfo.InvariantCulture, out startThreshold))
            {
                Logger.LogWarn("MainWindow.StartCapture_LoadUserInputValues", $"Can't parse TxtBox_StartThreshold.Text = {TxtBox_StartThreshold.Text} as float");

                success = false;
            }

            if (!int.TryParse(TxtBox_AnalyzeDuration.Text, CultureInfo.InvariantCulture, out analyzeDurationMs))
            {
                Logger.LogWarn("MainWindow.StartCapture_LoadUserInputValues", $"Can't parse TxtBox_AnalyzeDuration.Text = {TxtBox_AnalyzeDuration.Text} as int");

                success = false;
            }

            if (!float.TryParse(TxtBox_MaxAverageValue.Text, CultureInfo.InvariantCulture, out maxAverageValue))
            {
                Logger.LogWarn("MainWindow.StartCapture_LoadUserInputValues", $"Can't parse TxtBox_MaxAverageValue.Text = {TxtBox_MaxAverageValue.Text} as float");

                success = false;
            }

            if (success)
            {
                Config.MAIN_W_SAMPLE_BUFFER_DURATION_MS = sampleBufferDurationMs;
                Config.MAIN_W_START_THRESHOLD = startThreshold;
                Config.MAIN_W_ANALYZE_DURATION_MS = analyzeDurationMs;
                Config.MAIN_W_MAX_AVERAGE_VALUE = maxAverageValue;
            }

            return success;
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
            TxtBox_StartThreshold.IsEnabled = true;
            TxtBox_AnalyzeDuration.IsEnabled = true;
            TxtBox_MaxAverageValue.IsEnabled = true;

            SoundAnalyzer.Stop();
        }

        /// <summary>
        /// Verrify the user input for integer numbers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TxtBox_Validation_Number_Integer(object? sender, TextCompositionEventArgs e)
        {
            if (sender == null || sender is not TextBox)
            {
                e.Handled = false;
                return;
            }

            string fullInput = ((TextBox)sender).Text + e.Text;

            if (fullInput.Length == 0)
                fullInput = "0";

            int outVal = 0;

            bool success = int.TryParse(fullInput, CultureInfo.InvariantCulture, out outVal);

            e.Handled = !success;

            // e.Handled = false; => write new char
            // e.Handled = true;  => don't write new char
        }

        /// <summary>
        /// Verrify the user input for decimal numbers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TxtBox_Validation_Number_Decimal(object? sender, TextCompositionEventArgs e)
        {
            if (sender == null || sender is not TextBox)
            {
                e.Handled = false;
                return;
            }

            string fullInput = ((TextBox)sender).Text + e.Text;

            if (fullInput.Length == 0)
                fullInput = "0";

            if (fullInput.EndsWith('.'))
                fullInput += "0";

            float outVal = 0.0f;

            bool success = float.TryParse(fullInput, CultureInfo.InvariantCulture, out outVal);

            e.Handled = !success;

            // e.Handled = false; => write new char
            // e.Handled = true;  => don't write new char
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