using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Linq;

namespace WpfSoundMonitor
{
    /// <summary>
    /// Give a variety of tools to interact easily with the capture devices of the computer
    /// </summary>
    public static class CaptureDeviceManager
    {
        public static List<WasapiRecorder> WasapiRecorders { get; set; } = new List<WasapiRecorder>();
        public static WasapiRecorder? CurrentWasapiRecorder { get; set; } = null;

        /// <summary>
        /// Load audio capture devices from the OS
        /// </summary>
        /// <param name="deviceState">Device state used for filtering the results</param>
        /// <returns>True if no errors occured during the process</returns>
        public static bool LoadCaptureDevicesFromComputer(DeviceState deviceState = DeviceState.Active)
        {
            bool success = true;

            try
            {
                // clear the list if any capture device is already registered
                WasapiRecorders.ForEach(x => x.Dispose());
                WasapiRecorders = new List<WasapiRecorder>();

                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

                MMDeviceCollection mmcaptureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, deviceState);

                foreach (MMDevice? device in mmcaptureDevices)
                {
                    if (device == null)
                        continue;

                    WasapiRecorder wasapiRecorder = new WasapiRecorderBuilder()
                        .WithDevice(device)
                        .WithRawMode()
                        .WithLowLatency()
                        .Build();

                    WasapiRecorders.Add(wasapiRecorder);
                }
            }
            catch (Exception ex)
            {
                success = false;

                Debug.WriteLine($"> an error occured at {nameof(CaptureDeviceManager)} - {nameof(LoadCaptureDevicesFromComputer)} : ");
                Debug.WriteLine($"{ex.ToString()}");
            }

            return success;
        }

        /// <summary>
        /// Load a capture device to be used by the user
        /// </summary>
        /// <param name="deviceId">Device ID (WasapiRecorder.DeviceId)</param>
        /// <returns>True if the device specified has benn found</returns>
        public static bool LoadCaptureDeviceById(string deviceId)
        {
            WasapiRecorder? recorder = WasapiRecorders.FirstOrDefault(x => x.DeviceId == deviceId);

            if (recorder == null)
                return false;
            else
            {
                CurrentWasapiRecorder = recorder;

                return true;
            }
        }

        /// <summary>
        /// Clear the current capture device (set value to null)
        /// </summary>
        public static void ClearLoadedCaptureDevice()
        {
            CurrentWasapiRecorder = null;
        }
    }
}
