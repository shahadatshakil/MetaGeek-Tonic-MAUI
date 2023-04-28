using System;
using System.Globalization;
using System.Threading;

namespace MetaGeek.WiFi.Core.Helpers
{
    public static class Utils
    {
        public static DateTime GetLocalDateTimeFomattedValue(DateTime dateTime)
        {
            return TimeZone.CurrentTimeZone.ToLocalTime(dateTime);
        }

        public static string GetLocalTimeFormatStringLongValue(DateTime dateTime)
        {
            string systemTimeFormat = CultureInfo.CurrentUICulture.DateTimeFormat.LongTimePattern;
            return GetLocalDateTimeFomattedValue(dateTime).ToString(systemTimeFormat);
        }

        public static string GetLocalTimeFormatStringShortValue(DateTime dateTime)
        {
            string systemTimeFormat = CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern;
            return GetLocalDateTimeFomattedValue(dateTime).ToString(systemTimeFormat);
        }

        public static string TimeSpanToString(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 10)
                return "now";

            if (timeSpan.TotalSeconds < 60)
            {
                int seconds = (int)timeSpan.TotalSeconds;
                int roundedSeconds = seconds / 10 * 10;
                return $"{roundedSeconds} sec ago";
            }

            if (timeSpan.TotalMinutes < 60)
            {
                int minutes = (int)timeSpan.TotalMinutes;
                return $"{minutes} min ago";
            }

            if (timeSpan.TotalHours < 24)
            {
                int hours = (int)timeSpan.TotalHours;
                int minutes = (int)timeSpan.TotalMinutes % 60;
                return (hours > 1) ? $"{hours} hours and {minutes} min ago" : $"{hours} hour and {minutes} min ago";
            }

            int wholedays = timeSpan.Days;
            return (wholedays > 1) ? $"{wholedays} days ago" : $"{wholedays} day ago";
        }
    }
}
