using System;
using System.Diagnostics;
using System.Drawing;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using LedCSharp;

namespace keyboard_service
{
    public partial class KeyboardService : ServiceBase
    {
        private const int UPDATE_INTERVAL = 1000;
        private const int DARKNESS_THRESHOLD = 100;
        private const int BRIGHTNESS_THRESHOLD = 200;
        
        private static EventLog EventLog;
        private static string StoredBackground;
        private static int CurrentRedPerc;
        private static int CurrentGreenPerc;
        private static int CurrentBluePerc;

        public KeyboardService()
        {
            InitializeComponent();

            StoredBackground = "";

            // Initialize logger
            string _sourceName = "Keyboard Clock Service";
            string _logName = "Keyboard Clock Service Log";
            
            EventLog = new EventLog();
            this.AutoLog = false;

            if (!EventLog.SourceExists(_sourceName))
            {
                EventLog.CreateEventSource(_sourceName, _logName);
            }

            EventLog.Source = _sourceName;
            EventLog.Log = _logName;
        }

        protected override void OnStart(string[] args)
        {
            EventLog.WriteEntry("In OnStart.");

            bool _ledInitialized = LogitechGSDK.LogiLedInitWithName("Keyboard Color Service");

            if (!_ledInitialized)
            {
                this.Stop();
            }

            EventLog.WriteEntry("LED SDK Initialized");

            LogitechGSDK.LogiLedSetTargetDevice(LogitechGSDK.LOGI_DEVICETYPE_ALL);

            LogitechGSDK.LogiLedSetLighting(0, 0, 0);

            try
            {
                StoredBackground = DesktopBackgroundHelper.GetCurrentDesktopBackground();
                UpdateCurrentRGB();
                FadeUpFromBlack(2000);
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(e.Message + e.StackTrace, EventLogEntryType.Error);
            }
            
            System.Timers.Timer _timer = new System.Timers.Timer();
            _timer.AutoReset = true;
            _timer.Elapsed += new ElapsedEventHandler(CheckForBackgroundChange);
            _timer.Interval = UPDATE_INTERVAL;
            _timer.Start();
        }

        protected override void OnStop()
        {
            EventLog.WriteEntry("In OnStop.");
        }

        #region Methods

        private static void CheckForBackgroundChange(object sender, ElapsedEventArgs e)
        {
            string _currentBackground = DesktopBackgroundHelper.GetCurrentDesktopBackground();

            if (StoredBackground != _currentBackground)
            {
                EventLog.WriteEntry($"Background has been changed to {_currentBackground}. Updating keyboard...");
                StoredBackground = _currentBackground;
                FadeToBlack(1000);
                UpdateCurrentRGB();
                FadeUpFromBlack(1000);
            }
        }

        public static void FadeToBlack(int milliseconds)
        {
            int _redDecreaseStep = CurrentRedPerc / (milliseconds / 100);
            int _greenDecreaseStep = CurrentGreenPerc / (milliseconds / 100);
            int _blueDecreaseStep = CurrentBluePerc / (milliseconds / 100);

            // Increment by 100 milliseconds at a time
            for (int i = 100; i <= milliseconds; i+=100)
            {
                Thread.Sleep(100);
                CurrentRedPerc -= _redDecreaseStep;
                CurrentGreenPerc -= _greenDecreaseStep;
                CurrentBluePerc -= _blueDecreaseStep;
                LogitechGSDK.LogiLedSetLighting(CurrentRedPerc, CurrentGreenPerc, CurrentBluePerc);
            }

            CurrentRedPerc = 0;
            CurrentGreenPerc = 0;
            CurrentBluePerc = 0;

            LogitechGSDK.LogiLedSetLighting(CurrentRedPerc, CurrentGreenPerc, CurrentBluePerc);
        }

        public static void FadeUpFromBlack(int milliseconds)
        {
            int _redIncreaseStep = CurrentRedPerc / (milliseconds / 100);
            int _greenIncreaseStep = CurrentGreenPerc / (milliseconds / 100);
            int _blueIncreaseStep = CurrentBluePerc / (milliseconds / 100);

            int _redStepPerc = 0;
            int _greenStepPerc = 0;
            int _blueStepPerc = 0;

            // Increment by 100 milliseconds at a time
            for (int i = 100; i <= milliseconds; i+=100)
            {
                Thread.Sleep(100);
                _redStepPerc += _redIncreaseStep;
                _greenStepPerc += _greenIncreaseStep;
                _blueStepPerc += _blueIncreaseStep;
                LogitechGSDK.LogiLedSetLighting(_redStepPerc, _greenStepPerc, _blueStepPerc);
            }
            
            LogitechGSDK.LogiLedSetLighting(CurrentRedPerc, CurrentGreenPerc, CurrentBluePerc);
        }

        private static void UpdateCurrentRGB()
        {
            EventLog.WriteEntry($"Processing image: {StoredBackground}");

            try
            {
                long _sumRedVals = 0;
                long _sumGreenVals = 0;
                long _sumBlueVals = 0;
                long _pixelCount = 0;
            
                Bitmap _background = new Bitmap(StoredBackground);

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

                CurrentRedPerc = _redPerc;
                CurrentGreenPerc = _greenPerc;
                CurrentBluePerc = _bluePerc;

                EventLog.WriteEntry($"Finished processing image: {StoredBackground}");
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(e.Message + e.StackTrace, EventLogEntryType.Error);
            }
        }

        #endregion Methods
    }
}
