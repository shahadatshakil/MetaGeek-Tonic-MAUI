using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaGeek.WiFi.Core.Enums;

namespace MetaGeek.WiFi.Core
{
    public static class DataRateCalculator
    {
        #region Fields

        private static readonly double[,] BaseDataRates = {{6.5, 13.0, 19.5, 26.0, 39.0, 52.0, 58.5, 65.0, 78.0, 86.66666}, {7.22222, 14.44444, 21.66666, 28.88888, 43.33333, 57.77777, 65.0, 72.22222, 86.66666, 96.29629}};
        private static readonly double[] WidthMultipliers = {1, 13.5/6.5, 117.0/26.0, 9.0, 9.0};
        #endregion

        #region Methods

        public static double DataRateFromMcsDetails(int mcs, int streams, ChannelWidth width, bool sgiFlag)
        {
            var guardIndex = sgiFlag ? 1 : 0;

            var rate = BaseDataRates[guardIndex, mcs];
            rate *= streams;
            rate *= WidthMultipliers[(int) width];

            return rate;
        }
        #endregion
    }
}
