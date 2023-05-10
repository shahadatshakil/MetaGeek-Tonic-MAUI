using System;
using System.IO;

namespace MetaGeek.Tonic.Common.Utilities
{
    public static class ModelUtilities
    {
        //public static string GetFriendlyDeviceNameBasedOnWiSpyVersion(HardwareVersion hardwareVersion)
        //{
        //    switch (hardwareVersion)
        //    {
        //        case HardwareVersion.WiSpy24X:
        //        case HardwareVersion.WiSpy24X2:
        //            return "Wi-Spy 2.4x";

        //        case HardwareVersion.WiSpy900X2:
        //        case HardwareVersion.WiSpy900X:
        //            return "Wi-Spy 900x";

        //        case HardwareVersion.WiSpy950X:
        //            return "Wi-Spy 950x";

        //        case HardwareVersion.WiSpyDBx:
        //        case HardwareVersion.WiSpyDBx2:
        //        case HardwareVersion.WiSpyDBx3:
        //            return "Wi-Spy DBx";

        //        case HardwareVersion.WiSpyMini:
        //            return "Wi-Spy Mini";

        //        case HardwareVersion.Cisco:
        //            return "Cisco";

        //        default:
        //            return "";
        //    }
        //}
    }

    public static class DateTimeUtilities
    {
        private static DateTime UNIX_EPOCH = new DateTime(1970, 1, 1);

        public static long GetMillisecondsSinceEpoch(DateTime dateTime)
        {
            if (dateTime < UNIX_EPOCH) throw new ArgumentOutOfRangeException("dateTime", @"times before unixepoch are not supported");
            var mili = (dateTime - UNIX_EPOCH).TotalMilliseconds;

            return (long)mili;
        }

        public static DateTime GetDateTimeFromMilliseconds(double millisecondsSinceEpoch)
        {
            if (millisecondsSinceEpoch < 0) throw new ArgumentOutOfRangeException("millisecondsSinceEpoch", @"times before unixepoch are not supported");

            var dateTime = UNIX_EPOCH.AddMilliseconds(millisecondsSinceEpoch);
            return dateTime;
        }
    }
}
