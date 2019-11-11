using System;
using System.Diagnostics;
using System.Drawing;
using System.ServiceProcess;
using System.Timers;
using LedCSharp;

namespace keyboard_service
{
    public partial class KeyboardService : ServiceBase
    {
        private const int UPDATE_INTERVAL = 1000;
        private const int DARKNESS_THRESHOLD = 100;
        private const int BRIGHTNESS_THRESHOLD = 200;
        
        private static EventLog _eventLog;
        private static string _storedBackground;

        public KeyboardService()
        {
            InitializeComponent();

            _storedBackground = "";

            // Initialize logger
            string _sourceName = "Keyboard Clock Service";
            string _logName = "Keyboard Clock Service Log";
            
            _eventLog = new EventLog();
            this.AutoLog = false;

            if (!EventLog.SourceExists(_sourceName))
            {
                EventLog.CreateEventSource(_sourceName, _logName);
            }

            _eventLog.Source = _sourceName;
            _eventLog.Log = _logName;
        }

        protected override void OnStart(string[] args)
        {
            _eventLog.WriteEntry("In OnStart.");

            bool _ledInitialized = LogitechGSDK.LogiLedInitWithName("Keyboard Color Service");

            if (!_ledInitialized)
            {
                this.Stop();
            }

            _eventLog.WriteEntry("LED SDK Initialized");

            LogitechGSDK.LogiLedSetTargetDevice(LogitechGSDK.LOGI_DEVICETYPE_ALL);

            LogitechGSDK.LogiLedSetLighting(0, 0, 0);

            try
            {
                _storedBackground = DesktopBackgroundHelper.GetCurrentDesktopBackground();
                UpdateKeyboard();
            }
            catch (Exception e)
            {
                _eventLog.WriteEntry(e.Message + e.StackTrace, EventLogEntryType.Error);
            }
            
            Timer _timer = new Timer();
            _timer.AutoReset = true;
            _timer.Elapsed += new ElapsedEventHandler(CheckForBackgroundChange);
            _timer.Interval = UPDATE_INTERVAL;
            _timer.Start();
        }

        protected override void OnStop()
        {
            _eventLog.WriteEntry("In OnStop.");
        }

        #region Methods

        private static void CheckForBackgroundChange(object sender, ElapsedEventArgs e)
        {
            string _currentBackground = DesktopBackgroundHelper.GetCurrentDesktopBackground();

            if (_storedBackground != _currentBackground)
            {
                _eventLog.WriteEntry($"Background has been changed to {_currentBackground}. Updating keyboard...");
                _storedBackground = _currentBackground;
                UpdateKeyboard();
            }
        }

        private static void UpdateKeyboard()
        {
            _eventLog.WriteEntry($"Processing image: {_storedBackground}");

            try
            {
                long _sumRedVals = 0;
                long _sumGreenVals = 0;
                long _sumBlueVals = 0;
                long _pixelCount = 0;
            
                Bitmap _background = new Bitmap(_storedBackground);

                for (int y = 0; y < _background.Height; y++)
                {
                    for (int x = 0; x < _background.Width; x++)
                    {
                        Color _pixel = _background.GetPixel(x, y);

                        // If the pixel is not too dark and also not too bright, count it in the average
                        if (!(_pixel.R < DARKNESS_THRESHOLD && _pixel.G < DARKNESS_THRESHOLD && _pixel.B < DARKNESS_THRESHOLD) 
                            && !(_pixel.R > BRIGHTNESS_THRESHOLD && _pixel.G > BRIGHTNESS_THRESHOLD && _pixel.B > BRIGHTNESS_THRESHOLD))
                        {
                            _sumRedVals += _pixel.R;
                            _sumGreenVals += _pixel.G;
                            _sumBlueVals += _pixel.B;
                            _pixelCount++;
                        }
                    }
                }

                long _avgRed = _sumRedVals / _pixelCount;
                long _avgGreen = _sumGreenVals / _pixelCount;
                long _avgBlue = _sumBlueVals / _pixelCount;

                int _redPerc = (int)((double)_avgRed / (double)255 * 100);
                int _greenPerc = (int)((double)_avgGreen / (double)255 * 100);
                int _bluePerc = (int)((double)_avgBlue / (double)255 * 100);

                LogitechGSDK.LogiLedSetLighting((int)_redPerc, (int)_greenPerc, (int)_bluePerc);

                _eventLog.WriteEntry($"Finished processing image: {_storedBackground}");
            }
            catch (Exception e)
            {
                _eventLog.WriteEntry(e.Message + e.StackTrace, EventLogEntryType.Error);
            }
        }

        #endregion Methods
    }
}
