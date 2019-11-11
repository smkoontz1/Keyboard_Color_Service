using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace keyboard_service
{
    public static class DesktopBackgroundHelper
    {
        public static string GetCurrentDesktopBackground()
        {
            byte[] _backgroundPath = (byte[])Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop").GetValue("TranscodedImageCache");
            string _backgroundPathString = Encoding.Unicode.GetString(SliceMe(_backgroundPath, 24)).TrimEnd("\0".ToCharArray());
            return _backgroundPathString;
        }

        /// <summary>
        /// Pulled right from the internet.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static byte[] SliceMe(byte[] source, int pos)
        {
            byte[] _dest = new byte[source.Length - pos];
            Array.Copy(source, pos, _dest, 0, _dest.Length);
            return _dest;
        }
    }
}
