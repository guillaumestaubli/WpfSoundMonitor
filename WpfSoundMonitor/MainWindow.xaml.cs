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

namespace WpfSoundMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private RenderWindow SfmlWindow;
        private System.Windows.Forms.Control WPF_SFML_Surface;

        /// <summary>
        /// Default constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Create WIN32 surface in winforms for integration in WPF
            WPF_SFML_Surface = new System.Windows.Forms.Control();
            SfmlHost.Child = WPF_SFML_Surface;

            // Init SFML window for graphic rendering
            SfmlWindow = new RenderWindow(WPF_SFML_Surface.Handle);

            // add the handler for the loaded event of the WPF window
            this.Loaded += MainWindow_Loaded;

            // event for refreshing content of the SFML window
            CompositionTarget.Rendering += OnCompositionRendering;

            // event for managing the resize of the SFML render area in the WPF container
            WPF_SFML_Surface.Resize += WPF_SFML_Surface_Resized;
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
            SfmlWindow.Size = new Vector2u((uint)WPF_SFML_Surface.Width, (uint)WPF_SFML_Surface.Height);

            Vector2f position = new Vector2f(0, 0);
            Vector2f size = new Vector2f(WPF_SFML_Surface.Width, WPF_SFML_Surface.Height);

            View view = new View(new FloatRect(position, size));

            SfmlWindow.SetView(view);

            ScrollBarSfmlRenderArea.Width = WPF_SFML_Surface.Width;
        }

        /// <summary>
        /// SFML render loop synced with WPF render interval
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCompositionRendering(object? sender, EventArgs e)
        {
            if (SoundAnalyzer.IsCapturing)
                SoundWaveTracer.TraceSoundWave(SoundAnalyzer.SamplesAverageMaxValues.ToArray());

            if (SfmlWindow != null)
            {
                SfmlWindow.DispatchEvents();

                SfmlWindow.Clear(new SFML.Graphics.Color(30, 30, 30));

                // TODO : Draw graph representing sound captured by current capture device
                if (SoundWaveTracer.DrawableSoundWave != null)
                    SfmlWindow.Draw(SoundWaveTracer.DrawableSoundWave);

                SfmlWindow.Display();
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

        private void ScrollBarSfmlRenderArea_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

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

        
    }
}