using System;

namespace MetaGeek.Tonic.Common.Helpers
{
    public static class PacketHelpers
    {
        private static DateTime _epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static byte MICROSECOND_RESOLUTION = 6;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seconds">Seconds since Epoch</param>
        /// <param name="microseconds">Microseconds</param>
        /// <returns></returns>
        public static DateTime BuildDateTimeFromTsft(uint seconds, uint microseconds)
        {
            return _epochTime.AddSeconds(seconds).AddMilliseconds(microseconds / 1000);
        }

        public static DateTime BuildDateWithTimeResolution(uint timeHigh, uint timeLow, byte resolution)
        {
            var totalUnits = (ulong)timeHigh << 32 | timeLow;

            //if (resolution == MILLISECOND_RESOLUTION)
            {
                return _epochTime.AddMilliseconds(totalUnits / 1000);
            }
        }

        public static TimeSpan BuildTimeSpanFromMicroSeconds(uint microSeconds)
        {
            return TimeSpan.FromMilliseconds(microSeconds / 1000);
        }
    }
}
