using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using MetaGeek.WiFi.Core.Enums;
using MetaGeek.WiFi.Core.Interfaces;
using MetaGeek.WiFi.Core.Models;

namespace MetaGeek.WiFi.Core.Services
{
    /// <summary>
    /// Parses information elements from Beacons, Probe Request, Probe Response and Association Request
    /// </summary>
    public static class IEParser
    {
        #region Fields

        private const int MAX_SEC_BETWEEN_FULL_BEACON_PARSE = 10;
        private const ushort WLAN_FIXED_CPABILITIES_PRIVACY_BITMASK = 0x0010;

        private const byte BSS_BASIC_RATE_SET_MASK = 0x80;
        private const byte BSS_SUPPORTED_RATE_MASK = 0x7F;
        private const byte ERP_NON_ERP_PRESENT_MASK = 0x01;
        private const byte ERP_USE_PROTECTION_MASK = 0x02;
        private const byte HT_CAP_SUPPORTS_40_MHZ_MASK = 0x02;
        private const byte HT_INFO_NON_GREENFIELD_PRESENT_MASK = 0x04;
        private const byte HT_CAP_SHORT_GI_20_MASK = 0x20;
        private const byte HT_CAP_SHORT_GI_40_MASK = 0x40;
        private const byte VHT_CAP_STREAM_1_MCS_MAP_MASK = 0x03;
        private const byte VHT_NOT_SUPPORTED_STREAM_MCS_MAP = 0x03;
        private const byte VHT_OP_CHAN_WIDTH_20_40 = 0;
        private const byte VHT_OP_CHAN_WIDTH_80 = 1;
        private const byte VHT_OP_CHAN_WIDTH_160 = 2;
        private const byte VHT_OP_CHAN_WIDTH_80_80 = 3;
        private const byte VHT_CAP_SHORT_GI_80_MASK = 0x20;
        private const byte VHT_CAP_SHORT_GI_160_MASK = 0x40;

        private const byte CISCO_NAME_OFFSET = 10;
        private const byte CISCO_NAME_LENGTH = 16;

        private const byte VENDOR_ARUBA_NAME_OFFSET = 6;
        private const byte VENDOR_AEROHIVE_NAME_LENGTH_INDEX = 6;
        private const byte VENDOR_AEROHIVE_NAME_OFFSET = 7;
        private const byte AEROHIVE_NAME_TYPE = 33;
        private const byte MIST_NAME_TYPE = 1;
        private const byte VENDOR_MIST_NAME_OFFSET = 4;

        private const ulong EXT_CAP_BSS_TRANS_FLAG_MASK = 0x0000080000000000;

        
        private const byte HE_PHY_CAPABILITIES_LENGTH = 11;
        private const byte HE_OP_CHAN_WIDTH_20_40 = 0x02;
        private const byte HE_OP_CHAN_WIDTH_80 = 0x04;
        private const byte HE_OP_CHAN_WIDTH_160 = 0x08;
        private const byte HE_OP_CHAN_WIDTH_80_80 = 0x10;
        private const byte HE_NOT_SUPPORTED_STREAM_MCS_MAP = 0x03;
        private const ushort HE_CAP_0_8_GI_MASK = 0x0040;
        private const ushort HE_CAP_3_2_GI_MASK = 0x0200;



        // Organized by channel width, Spatial stream, MCS. First value is 20 MHz, 1 SS, MCS 0 followed by 20 MHz, 1 SS, MCS 1... up to 160 MHz, 3 SS, MCS 9
        public static readonly double[] DATA_RATE_LGI_LOOKUP_TABLE = 
        {
            // 20 MHz
            6.5, 13, 19.5, 26, 39, 52, 58.5, 65, 78, 86.7,
            13, 26, 39, 52, 78, 104, 117, 130, 156, 173.3,
            19.5, 39, 58.5, 78, 117, 156, 175.5, 195, 234, 260,
            26, 52, 78, 104, 156, 208, 234, 260, 312, 346.7,
            // 40 MHz
            13.5, 27, 40.5, 54, 81, 108, 121.5, 135, 162, 180,
            27, 54, 81, 108, 162, 216, 243, 270, 324, 360,
            40.5, 81, 121.5, 162, 243, 324, 364.5, 405, 486, 540,
            54, 108, 162, 216, 324, 432, 486, 540, 748, 720,
            // 80 MHz
            29.3, 58.5, 87.8, 117, 175.5, 234, 263.3, 292.5, 351, 390,
            58.5, 117, 175.5, 234, 351, 468, 526.5, 585, 702, 780,
            87.8, 175.5, 263.3, 351, 526.5, 702, 780, 877.5, 1053, 1170,
            117, 234, 351, 468, 702, 936, 1053, 1170, 1404, 1560,
            // 160 MHz
            58.5, 117, 175.5, 234, 351, 468, 526.5, 585, 702, 780,
            117, 234, 351, 468, 702, 936, 1053, 1170, 1404, 1560,
            175.5, 351, 526.5, 702, 1053, 1404, 1579.5, 1755, 2106, 2340,
            234, 468, 702, 936, 1404, 1872, 2106, 2340, 2808, 3120,
        };

        public static readonly double[] DATA_RATE_SGI_LOOKUP_TABLE =
        {
            // 20 MHz
            7.2, 14.4, 21.7, 28.9, 43.3, 57.8, 65, 72.2, 86.7, 96.3,
            14.4, 28.9, 43.3, 57.8, 86.7, 115.6, 130.3, 144.4, 173.3, 192.6,
            21.7, 43.3, 65, 86.7, 130, 173.3, 195, 216.7, 260, 288.9,
            28.9, 57.8, 86.7, 115.6, 173.3, 231.1, 260, 288.9, 346.7, 385.2,
            // 40 MHz
            15, 30, 45, 60, 90, 120, 135, 150, 180, 200,
            30, 60, 90, 120, 180, 240, 270, 300, 360, 400,
            45, 90, 135, 180, 270, 360, 405, 450, 540, 600,
            60, 120, 180, 240, 360, 480, 540, 600, 720, 800,
            // 80 MHz
            32.5, 65, 97.5, 130, 195, 260, 292.5, 325, 390, 433.3,
            65, 130, 195, 260, 390, 520, 585, 650, 780, 866.7,
            97.5, 195, 292.5, 390, 585, 780, 877.5, 975, 1170, 1300,
            130, 260, 390, 520, 780, 1040, 1170, 1300, 1560, 1733.3,
            // 160 MHz
            65, 130, 195, 260, 390, 520, 585, 650, 780, 866.7,
            130, 260, 390, 520, 780, 1040, 1170, 1300, 1560, 1733.3,
            195, 390, 585, 780, 1170, 1560, 1755, 1950, 2340, 2600,
            260, 520, 780, 1040, 1560, 2080, 2340, 2600, 3120, 3466.7,
        };

        public static readonly double[] HE_DATA_RATE_SGI_LOOKUP_TABLE =
        {
            // 20 MHz
            8.6, 17.2, 25.8, 34.4, 51.6, 68.8, 77.4, 86, 103.2, 114.7, 129, 143.4, 
            17.2, 34.4, 51.6, 68.8, 103.2, 137.6, 154.9, 172.1, 206.5, 229.4, 258.1, 286.8, 
            25.8, 51.6, 77.4, 103.2, 154.9, 206.5, 232.3, 258.1, 309.7, 344.1, 387.1, 430.1, 
            34.4, 68.8, 103.2, 137.6, 206.5, 275.3, 309.7, 344.1, 412.9, 458.8, 516.2, 573.5, 
            43, 86, 129, 172.1, 258.1, 344.1, 387.1, 430.1, 516.2, 573.5, 645.2, 716.9, 
            51.6, 103.2, 154.9, 206.5, 309.7, 412.9, 464.6, 516.2, 619.4, 688.2, 774.3, 860.3,
            60.2, 120.4, 180.7, 240.9, 361.3, 481.8, 542, 602.2, 722.6, 802.9, 903.3, 1003.7, 
            68.8, 137.6, 206.5, 275.3, 412.9, 550.6, 619.4, 688.2, 825.9, 917.6, 1032.4, 1147.1, 
            // 40 MHz
            17.2, 34.4, 51.6, 68.8, 103.2, 137.6, 154.9, 172.1, 206.5, 229.4, 258.1, 286.8,
            34.4, 68.8, 103.2, 137.6, 206.5, 275.3, 309.7, 344.1, 412.9, 458.8, 516.2, 573.5, 
            51.6, 103.2, 154.9, 206.5, 309.7, 412.9, 464.6, 516.2, 619.4, 688.2, 774.3, 860.3, 
            68.8, 137.6, 206.5, 275.3, 412.9, 550.6, 619.4, 688.2, 825.9, 917.6, 1032.4, 1147.1,
            86, 172.1, 258.1, 344.1, 516.2, 688.2, 774.3, 860.3, 1032.4, 1147.1, 1290.4, 1433.8, 
            103.2, 206.5, 309.7, 412.9, 619.4, 825.9, 929.1, 1032.4, 1238.8, 1376.5, 1548.5, 1720.6, 
            120.4, 240.9, 361.3, 481.8, 722.6, 963.5, 1084, 1204.4, 1445.3, 1605.9, 1806.6, 2007.4, 
            137.6, 275.3, 412.9, 550.6, 825.9, 1101.2, 1238.8, 1376.5, 1651.8, 1835.3, 2064.7, 2294.1,
            // 80 MHz
            36, 72.1, 108.1, 144.1, 216.2, 288.2, 324.3, 360.3, 432.4, 480.4, 540.4, 600.5, 
            72.1, 144.1, 216.2, 288.2, 432.4, 576.5, 648.5, 720.6, 864.7, 960.8, 1080.9, 1201, 
            108.1, 216.2, 324.3, 432.4, 648.5, 864.7, 972.8, 1080.9, 1297.1, 1441.2, 1621.3, 1801.5, 
            144.1, 288.2, 432.4, 576.5, 864.7, 1152.9, 1297.1, 1441.2, 1729.4, 1921.6, 2161.8, 2402, 
            180.1, 360.3, 540.4, 720.6, 1080.9, 1441.2, 1621.3, 1801.5, 2161.8, 2402, 2702.2, 3002.5, 
            216.2, 432.4, 648.5, 864.7, 1297.1, 1729.4, 1945.6, 2161.8, 2594.1, 2882.4, 3242.6, 3602.9, 
            252.2, 504.4, 756.6, 1008.8, 1513.2, 2017.6, 2269.9, 2522.1, 3026.5, 3362.7, 3783.1, 4203.4, 
            288.2, 576.5, 864.7, 1152.9, 1729.4, 2305.9, 2594.1, 2882.4, 3458.8, 3843.1, 4323.5, 4803.9, 
            // 160 MHz
            72.1, 144.1, 216.2, 288.2, 432.4, 576.5, 648.5, 720.6, 864.7, 960.8, 1080.9, 1201, 
            144.1, 288.2, 432.4, 576.5, 864.7, 1152.9, 1297.1, 1441.2, 1729.4, 1921.6, 2161.8, 2402, 
            216.2, 432.4, 648.5, 864.7, 1297.1, 1729.4, 1945.6, 2161.8, 2594.1, 2882.4, 3242.6, 3602.9,
            288.2, 576.5, 864.7, 1152.9, 1729.4, 2305.9, 2594.1, 2882.4, 3458.8, 3843.1, 4323.5, 4803.9, 
            360.3, 720.6, 1080.9, 1441.2, 2161.8, 2882.4, 3242.6, 3602.9, 4323.5, 4803.9, 5404.4, 6004.9, 
            432.4, 864.7, 1297.1, 1729.4, 2594.1, 3458.8, 3891.2, 4323.5, 5188.2, 5764.7, 6485.3, 7205.9, 
            504.4, 1008.8, 1513.2, 2017.6, 3026.5, 4035.3, 4539.7, 5044.1, 6052.9, 6725.5, 7566.2, 8406.9, 
            576.5, 1152.9, 1729.4, 2305.9, 3458.8, 4611.8, 5188.2, 5764.7, 6917.6, 7686.3, 8647.1, 9607.8
        };

        public static readonly double[] HE_DATA_RATE_MGI_LOOKUP_TABLE =
        {
            // 20 MHz
            8.1, 16.3, 24.4, 32.5, 48.8, 65, 73.1, 81.3, 97.5, 108.3, 121.9, 135.4,
            16.3, 32.5, 48.8, 65, 97.5, 130, 146.3, 162.5, 195, 216.7, 243.8, 270.8,
            24.4, 48.8, 73.1, 97.5, 146.3, 195, 219.4, 243.8, 292.5, 325, 365.6, 406.3,
            32.5, 65, 97.5, 130, 195, 260, 292.5, 325, 390, 433.3, 487.5, 541.7,
            40.6, 81.3, 121.9, 162.5, 243.8, 325, 365.6, 406.3, 487.5, 541.7, 609.4, 677.1,
            48.8, 97.5, 146.3, 195, 292.5, 390, 438.8, 487.5, 585, 650, 731.3, 812.5,
            56.9, 113.8, 170.6, 227.5, 341.3, 455, 511.9, 568.8, 682.5, 758.3, 853.1, 947.9,
            65, 130, 195, 260, 390, 520, 585, 650, 780, 866.7, 975, 1083.3,
            // 40 MHz
            16.3, 32.5, 48.8, 65, 97.5, 130, 146.3, 162.5, 195, 216.7, 243.8, 270.8,
            32.5, 65, 97.5, 130, 195, 260, 292.5, 325, 390, 433.3, 487.5, 541.7,
            48.8, 97.5, 146.3, 195, 292.5, 390, 438.8, 487.5, 585, 650, 731.3, 812.5,
            65, 130, 195, 260, 390, 520, 585, 650, 780, 866.7, 975, 1083.3,
            81.3, 162.5, 243.8, 325, 487.5, 650, 731.3, 812.5, 975, 1083.3, 1218.8, 1354.2,
            97.5, 195, 292.5, 390, 585, 780, 877.5, 975, 1170, 1300, 1462.5, 1625,
            113.8, 227.5, 341.3, 455, 682.5, 910, 1023.8, 1137.5, 1365, 1516.7, 1706.3, 1895.8,
            130, 260, 390, 520, 780, 1040, 1170, 1300, 1560, 1733.3, 1950, 2166.7,
            // 80 MHz
            34, 68.1, 102.1, 136.1, 204.2, 272.2, 306.3, 340.3, 408.3, 453.7, 510.4, 567.1,
            68.1, 136.1, 204.2, 272.2, 408.3, 544.4, 612.5, 680.6, 816.7, 907.4, 1020.8, 1134.3,
            102.1, 204.2, 306.3, 408.3, 612.5, 816.7, 918.8, 1020.8, 1225, 1361.1, 1531.3, 1701.4,
            136.1, 272.2, 408.3, 544.4, 816.7, 1088.9, 1225, 1361.1, 1633.3, 1814.8, 2041.7, 2268.5, 
            170.1, 340.3, 510.4, 680.6, 1020.8, 1361.1, 1531.3, 1701.4, 2041.7, 2268.5, 2552.1, 2835.6,
            204.2, 408.3, 612.5, 816.7, 1225, 1633.3, 1837.5, 2041.7, 2450, 2722.2, 3062.5, 3402.8,
            238.2, 476.4, 714.6, 952.8, 1429.2, 1905.6, 2143.8, 2381.9, 2858.3, 3175.9, 3572.9, 3969.9, 
            272.2, 544.4, 816.7, 1088.9, 1633.3, 2177.8, 2450, 2722.2, 3266.7, 3629.6, 4083.3, 4537,
            // 160 MHz
            68.1, 136.1, 204.2, 272.2, 408.3, 544.4, 612.5, 680.6, 816.7, 907.4, 1020.8, 1134.3,
            136.1, 272.2, 408.3, 544.4, 816.7, 1088.9, 1225, 1361.1, 1633.3, 1814.8, 2041.7, 2268.5, 
            204.2, 408.3, 612.5, 816.7, 1225, 1633.3, 1837.5, 2041.7, 2450, 2722.2, 3062.5, 3402.8, 
            272.2, 544.4, 816.7, 1088.9, 1633.3, 2177.8, 2450, 2722.2, 3266.7, 3629.6, 4083.3, 4537, 
            340.3, 680.6, 1020.8, 1361.1, 2041.7, 2722.2, 3062.5, 3402.8, 4083.3, 4537, 5104.2, 5671.3, 
            408.3, 816.7, 1225, 1633.3, 2450, 3266.7, 3675, 4083.3, 4900, 5444.4, 6125, 6805.6, 
            476.4, 952.8, 1429.2, 1905.6, 2858.3, 3811.1, 4287.5, 4763.9, 5716.7, 6351.9, 7145.8, 7939.8, 
            544.4, 1088.9, 1633.3, 2177.8, 3266.7, 4355.6, 4900, 5444.4, 6533.3, 7259.3, 8166.7, 9074.1
        };

        public static readonly double[] HE_DATA_RATE_LGI_LOOKUP_TABLE =
        {
            // 20 MHz
            7.3, 14.6, 21.9, 29.3, 43.9, 58.5, 65.8, 73.1, 87.8, 97.5, 109.7, 121.9,
            14.6, 29.3, 43.9, 58.5, 87.8, 117, 131.6, 146.3, 175.5, 195, 219.4, 243.8,
            21.9, 43.9, 65.8, 87.8, 131.6, 175.5, 197.4, 219.4, 263.3, 292.5, 329.1, 365.6, 
            29.3, 58.5, 87.8, 117, 175.5, 234, 263.3, 292.5, 351, 390, 438.8, 487.5,
            36.6, 73.1, 109.7, 146.3, 219.4, 292.5, 329.1, 365.6, 438.8, 487.5, 548.4, 609.4,
            43.9, 87.8, 131.6, 175.5, 263.3, 351, 394.9, 438.8, 526.5, 585, 658.1, 731.3,
            51.2, 102.4, 153.6, 204.8, 307.1, 409.5, 460.7, 511.9, 614.3, 682.5, 767.8, 853.1,
            58.5, 117, 175.5, 234, 351, 468, 526.5, 585, 702, 780, 877.5, 975,
            // 40 MHz
            14.6, 29.3, 43.9, 58.5, 87.8, 117, 131.6, 146.3, 175.5, 195, 219.4, 243.8, 
            29.3, 58.5, 87.8, 117, 175.5, 234, 263.3, 292.5, 351, 390, 438.8, 487.5, 
            43.9, 87.8, 131.6, 175.5, 263.3, 351, 394.9, 438.8, 526.5, 585, 658.1, 731.3, 
            58.5, 117, 175.5, 234, 351, 468, 526.5, 585, 702, 780, 877.5, 975,
            73.1, 146.3, 219.4, 292.5, 438.8, 585, 658.1, 731.3, 877.5, 975, 1096.9, 1218.8,
            87.8, 175.5, 263.3, 351, 526.5, 702, 789.8, 877.5, 1053, 1170, 1316.3, 1462.5,
            102.4, 204.8, 307.1, 409.5, 614.3, 819, 921.4, 1023.8, 1228.5, 1365, 1535.6, 1706.3, 
            117, 234, 351, 468, 702, 936, 1053, 1170, 1404, 1560, 1755, 1950,
            // 80 MHz
            30.6, 61.3, 91.9, 122.5, 183.8, 245, 275.6, 306.3, 367.5, 408.3, 459.4, 510.4,
            61.3, 122.5, 183.8, 245, 367.5, 490, 551.3, 612.5, 735, 816.7, 918.8, 1020.8, 
            91.9, 183.8, 275.6, 367.5, 551.3, 735, 826.9, 918.8, 1102.5, 1225, 1378.1, 1531.3,
            122.5, 245, 367.5, 490, 735, 980, 1102.5, 1225, 1470, 1633.3, 1837.5, 2041.7, 
            153.1, 306.3, 459.4, 612.5, 918.8, 1225, 1378.1, 1531.3, 1837.5, 2041.7, 2296.9, 2552.1, 
            183.8, 367.5, 551.3, 735, 1102.5, 1470, 1653.8, 1837.5, 2205, 2450, 2756.3, 3062.5, 
            214.4, 428.8, 643.1, 857.5, 1286.3, 1715, 1929.4, 2143.8, 2572.5, 2858.3, 3215.6, 3572.9,
            245, 490, 735, 980, 1470, 1960, 2205, 2450, 2940, 3266.7, 3675, 4083.3,
            // 160 MHz
            61.3, 122.5, 183.8, 245, 367.5, 490, 551.3, 612.5, 735, 816.7, 918.8, 1020.8, 
            122.5, 245, 367.5, 490, 735, 980, 1102.5, 1225, 1470, 1633.3, 1837.5, 2041.7, 
            183.8, 367.5, 551.3, 735, 1102.5, 1470, 1653.8, 1837.5, 2205, 2450, 2756.3, 3062.5,
            245, 490, 735, 980, 1470, 1960, 2205, 2450, 2940, 3266.7, 3675, 4083.3,
            306.3, 612.5, 918.8, 1225, 1837.5, 2450, 2756.3, 3062.5, 3675, 4083.3, 4593.8, 5104.2,
            367.5, 735, 1102.5, 1470, 2205, 2940, 3307.5, 3675, 4410, 4900, 5512.5, 6125, 
            428.8, 857.5, 1286.3, 1715, 2572.5, 3430, 3858.8, 4287.5, 5145, 5716.7, 6431.3, 7145.8, 
            490, 980, 1470, 1960, 2940, 3920, 4410, 4900, 5880, 6533.3, 7350, 8166.7
        };
        #endregion

        #region Methods

        /// <summary>
        /// Updates only information elements that change regularly
        /// </summary>
        /// <param name="bssidDetails"></param>
        /// <param name="informationElementBytes"></param>
        /// <param name="scanTimeStamp"></param>
        public static void UpdateBeaconInformationElements(IBssidDetails bssidDetails, byte[] informationElementBytes, DateTime scanTimeStamp)
        {
            if (bssidDetails == null || informationElementBytes == null) return;

            var informationElements = BuildInformationElements(informationElementBytes);
            if (informationElements == null) return;

            bool parseFullBeaconFlag = bssidDetails.ItsLastFullBeaconParseTime.AddSeconds(MAX_SEC_BETWEEN_FULL_BEACON_PARSE) < scanTimeStamp;

            if (!parseFullBeaconFlag)
            {
                foreach (var informationElement in informationElements)
                {
                    switch (informationElement.ItsId)
                    {
                        case InformationElementId.BssLoad:
                            ParseBssLoadElement(bssidDetails, informationElement);
                            break;

                        case InformationElementId.DsParameterSet:
                            if (informationElement.ItsLength != 1)
                            {
                                continue;
                            }

                            var channel = informationElement.ItsData[0];
                            if (channel != bssidDetails.ItsChannelInfo.ItsPrimaryChannel)
                            {
                                parseFullBeaconFlag = true;
                            }

                            break;
                    }
                }
            }

            if (parseFullBeaconFlag)
            {
                ParseAllBeaconInformationElements(bssidDetails, informationElementBytes, scanTimeStamp);
            }
        }

        /// <summary>
        /// Updates ALL information elements that we are concerned with
        /// </summary>
        /// <param name="bssidDetails"></param>
        /// <param name="informationElementBytes"></param>
        public static void ParseAllBeaconInformationElements(IBssidDetails bssidDetails, byte[] informationElementBytes, DateTime scanTimeStamp)
        {
            Contract.Requires(bssidDetails != null);
            Contract.Requires(informationElementBytes != null);

            var informationElements = BuildInformationElements(informationElementBytes);
            if (informationElements == null) return;

            bssidDetails.ItsLastFullBeaconParseTime = scanTimeStamp;

            foreach (var informationElement in informationElements)
            {
                switch (informationElement.ItsId)
                {
                    case InformationElementId.Ssid:
                        bssidDetails.ItsSsid = Encoding.UTF8.GetString(informationElement.ItsData).TrimEnd('\0');
                        break;

                    case InformationElementId.SupportedRates:
                        ParseSupportedRatesElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.DsParameterSet:
                        ParseDsParameterElement(bssidDetails, informationElement);
                        break;

                    // Not parsing TIM
                    case InformationElementId.TrafficIndicationMap:
                        break;

                    case InformationElementId.CountryInformation:
                        ParseCountryInformationElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.BssLoad:
                        ParseBssLoadElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.ErpParameter:
                        ParseErpElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.RobustSecurityNetwork:
                        ParseRobustSecurityElement(bssidDetails, informationElement);
                        bssidDetails.ItsHasRSNElement = true;
                        break;

                    case InformationElementId.ExtendedRates:
                        ParseExtendedRatesElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.ExtendedCapabilities:
                        ParseExtendedCapabilitiesElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.HtCapabilities:
                        ParseHtCapabilitiesElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.HtInformation:
                        ParseHtInformationElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.VHtCapabilities:
                        ParseVhtCapabilitiesElement(bssidDetails, informationElement, bssidDetails.ItsChannelInfo.ItsBand);
                        break;

                    case InformationElementId.VHtOperation:
                        ParseVhtOperationElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.CiscoDeviceName:
                        ParseCiscoDeviceNameElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.VendorSpecific:
                        ParseVendorSpecificElement(bssidDetails, informationElement);
                        break;

                    case InformationElementId.Extension:
                        ParseExtensionElement(bssidDetails, informationElement);
                        break;
                    case InformationElementId.RmCapabilities:
                        ParseRmCapabilitiesElement(bssidDetails, informationElement);
                        break;
                    default:
                        //Trace.WriteLine($"Ignore IE ID: {informationElement.ItsId}");
                        break;
                }
            }

            PostProcessBeaconInformation(bssidDetails);

            bssidDetails.ItsTaxonomySignature = BuildTaxonomySignature(bssidDetails, informationElements);
        }

        /// <summary>
        /// Parses information elements from a client - packet is either probe request or association request
        /// Association Requests tend to have more information elements and thus a more unique signature
        /// </summary>
        /// <param name="clientDetails"></param>
        /// <param name="informationElementBytes"></param>
        /// <param name="isProbe"></param>
        public static void ParseClientInformationElements(IClientDetails clientDetails, byte[] informationElementBytes, bool isProbe, ChannelBand band)
        {
            if (clientDetails == null || informationElementBytes == null) return;

            var informationElements = BuildInformationElements(informationElementBytes);
            if (informationElements == null) return;

            foreach (var informationElement in informationElements)
            {
                switch (informationElement.ItsId)
                {
                    case InformationElementId.Ssid:
                        var ssid = Encoding.UTF8.GetString(informationElement.ItsData).TrimEnd('\0');
                        if (isProbe)
                        {
                            clientDetails.AddProbedNetwork(ssid);
                        }
                        else
                        {
                            clientDetails.ItsDisplaySsid = ssid;
                        }
                        break;

                    case InformationElementId.SupportedRates:
                        ParseSupportedRatesElement(clientDetails, informationElement);
                        break;

                    case InformationElementId.RobustSecurityNetwork:
                        ParseRobustSecurityElement(clientDetails, informationElement);
                        break;

                    case InformationElementId.HtCapabilities:
                        ParseHtCapabilitiesElement(clientDetails, informationElement);
                        break;

                    case InformationElementId.VHtCapabilities:
                        ParseVhtCapabilitiesElement(clientDetails, informationElement, band);
                        break;

                    case InformationElementId.ExtendedCapabilities:
                        ParseExtendedCapabilitiesElement(clientDetails, informationElement);
                        break;

                    case InformationElementId.RmCapabilities:
                        ParseRmCapabilitiesElement(clientDetails, informationElement);
                        break;

                    case InformationElementId.PowerCapability:
                    case InformationElementId.SupportedChannels:
                        break;
                }
            }

            if (isProbe)
            {
                if (band == ChannelBand.TwoGhz)
                {
                    clientDetails.Its24GhzProbeSignature = BuildTaxonomySignature(clientDetails, informationElements);
                }
                else
                {
                    clientDetails.Its5GhzProbeSignature = BuildTaxonomySignature(clientDetails, informationElements);
                }
            }
            else
            {
                clientDetails.ItsTaxonomySignature = BuildTaxonomySignature(clientDetails, informationElements);
            }
        }

        public static string GetPacketSsid(byte[] informationElementBytes)
        {
            if (informationElementBytes == null) return null;
            var informationElements = BuildInformationElements(informationElementBytes);
            if (informationElements == null) return null;
            var information = informationElements.FirstOrDefault(x => x.ItsId == InformationElementId.Ssid);
            if (information == null) return null;
            var ssid = Encoding.UTF8.GetString(information.ItsData).TrimEnd('\0');

            return ssid == string.Empty ? null : ssid;
        }

        public static List<IMacAddress> GetPacketBssidMacList(byte[] informationElementBytes)
        {
            if (informationElementBytes == null)
                return null;

            var informationElements = BuildInformationElements(informationElementBytes);
            var information = informationElements?.FindAll(x => x.ItsId == InformationElementId.NeighborReport);

            if (information == null || information.Count == 0)
                return null;

            List<IMacAddress> bssidMacList = new List<IMacAddress>();

            foreach (var ie in information)
            {
                bssidMacList.Add(MacAddressCollection.GetMacAddress(ie.ItsData, 0));
            }

            return bssidMacList;
        }

        /// <summary>
        /// Builds the taxonomy signature from the list of information elements
        /// </summary>
        /// <param name="deviceDetails"></param>
        /// <param name="informationElements"></param>
        /// <returns></returns>
        public static string BuildTaxonomySignature(IBaseDeviceDetails deviceDetails, List<InformationElement> informationElements)
        {
            Contract.Requires(deviceDetails != null);
            Contract.Requires(informationElements != null);

            var builder = new StringBuilder();
            ushort txpow = 0;
            byte[] extcap = new byte[] { };
            byte htagg = 0;
            uint htmcs = 0;
            uint vhtrxmcs = 0;
            uint vhttxmcs = 0;

            try
            {
                foreach (var element in informationElements)
                {
                    var ieData = element.ItsData;

                    // ignore RSN element, it is too transient to be relied upon in a taxonomy signature
                    if (element.ItsId == InformationElementId.RobustSecurityNetwork) continue;
                    // ignore TIM which is sometimes only sent by BSSIDs with clients and not guaranteed to always exist or not exist.
                    if (element.ItsId == InformationElementId.TrafficIndicationMap) continue;

                    if (builder.Length > 0)
                    {
                        builder.Append(",");
                    }

                    builder.Append((int) element.ItsId);
                    if (element.ItsId == InformationElementId.VendorSpecific && element.ItsLength > 4)
                    {
                        var test = ReadUintFromNetworkOrderByteArray(ieData, 0, 3);
                        var oui = ((uint) (ieData[0]) << 16) + ((uint) (ieData[1] << 8)) + ieData[2];
                        var ouiType = ieData[3];
                        builder.AppendFormat("({0:x6},{1})", oui, ouiType);
                    }
                    else
                    {
                        switch (element.ItsId)
                        {
                            case InformationElementId.PowerCapability:
                                txpow = (ushort) ((ushort) (ieData[1] << 8) + ieData[0]);
                                break;

                            case InformationElementId.ExtendedCapabilities:
                                extcap = ieData;
                                break;

                            case InformationElementId.HtCapabilities:
                                htagg = ieData[2];
                                htmcs = ReadUintFromNetworkOrderByteArray(ieData, 3, 4);
                                break;

                            case InformationElementId.VHtCapabilities:
                                vhtrxmcs = ReadUintFromNetworkOrderByteArray(ieData, 4, 4);
                                vhttxmcs = ReadUintFromNetworkOrderByteArray(ieData, 8, 4);
                                break;
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                return string.Empty;
            }

            if (deviceDetails.ItsPhyTypeInfo.ItsHtCapabilitiesInfo > 0)
            {
                builder.AppendFormat(",htcap:{0:x4}", deviceDetails.ItsPhyTypeInfo.ItsHtCapabilitiesInfo);
            }
            if (htagg > 0)
            {
                builder.AppendFormat(",htagg:{0:x2}", htagg);
            }
            if (htmcs > 0)
            {
                builder.AppendFormat(",htmcs:{0:x8}", htmcs);
            }
            if (deviceDetails.ItsPhyTypeInfo.ItsVhtCapabilitiesInfo > 0)
            {
                builder.AppendFormat(",vhtcap:{0:x8}", deviceDetails.ItsPhyTypeInfo.ItsVhtCapabilitiesInfo);
            }
            if (vhtrxmcs > 0)
            {
                builder.AppendFormat(",vhtrxmcs:{0:x8}", vhtrxmcs);
            }
            if (vhttxmcs > 0)
            {
                builder.AppendFormat(",vhttxmcs:{0:x8}", vhttxmcs);
            }
            if (txpow > 0)
            {
                builder.AppendFormat(",txpow:{0:x4}", txpow);
            }
            if (extcap.Length > 0)
            {
                builder.Append(",extcap:");
                foreach (var b in extcap)
                {
                    builder.AppendFormat("{0:x2}", b);
                }
            }

            return builder.ToString();
        }

        private static uint ReadUintFromNetworkOrderByteArray(Byte[] data, int startIndex, int bytes, bool littleEndian = true)
        {
            var lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to read a " + bytes + "-byte value at offset " + startIndex + ".");
            
            if (bytes > 4)
                throw new OverflowException("Reading " + bytes + "-byte value will cause an overflow.");
            
            uint value = 0;
            for (var index = 0; index < bytes; index++)
            {
                var offset = startIndex + (littleEndian ? index : lastByte - index);
                value += (uint)data[offset] << (8 * index);
            }

            return value;
        }

        private static void ParseVendorSpecificElement(IBssidDetails bssidDetails,
            InformationElement informationElement)
        {
            // Must be long enough to have data after the OUI and OUI Type fields
            if (informationElement.ItsLength <= 4) return;

            var ieData = informationElement.ItsData;
            var oui = ((uint) (ieData[0]) << 16) + ((uint) (ieData[1] << 8)) + ieData[2];

            switch (oui)
            {
                // TODO Are all these OUIs actually used by Aruba? Most vendors just use ONE OUI for their vendor specific IEs
                case 0x000B86:
                case 0x001A1E:
                case 0x00246C:
                case 0x186472:
                case 0x24DEC6:
                case 0x6CF37F:
                case 0x9C1C12:
                case 0xD8C7C8:
                    ParseArubaVendorElement(bssidDetails, informationElement);
                    break;

                case 0x001977:
                    ParseAerohiveVendorElement(bssidDetails, informationElement);
                    break;

                case 0x0050F2:
                    ParseWepWpaElement(bssidDetails, informationElement);
                    break;

                case 0x5C5B35:
                    ParseMistVendorElement(bssidDetails, informationElement);
                    break;

                default:
                    break;

            }
        }

        private static void ParseMistVendorElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            var ieData = informationElement.ItsData;
            var ouiType = ieData[3];

            if (ouiType != MIST_NAME_TYPE) return;

            var nameLength = informationElement.ItsLength - VENDOR_MIST_NAME_OFFSET; // OUI 3 bytes, Type 1 byte
            var nameBytes = new byte[nameLength];

            Array.Copy(ieData, VENDOR_MIST_NAME_OFFSET, nameBytes, 0, nameLength);
            if (DeviceNameIsPrintable(nameBytes))
            {
                bssidDetails.ItsBroadcastName = Encoding.UTF8.GetString(nameBytes);
            }
        }

        private static void ParseAerohiveVendorElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            var ieData = informationElement.ItsData;
            var ouiType = ieData[3];

            if (ouiType != AEROHIVE_NAME_TYPE) return; // Aerohive Type 33 is Device Name, we don't parse any other Vendor Specific Aerohive Information Elements

            var nameLength = ieData[VENDOR_AEROHIVE_NAME_LENGTH_INDEX] - 1;
            var nameBytes = new byte[nameLength];

            Array.Copy(ieData, VENDOR_AEROHIVE_NAME_OFFSET, nameBytes, 0, nameLength);
            if (DeviceNameIsPrintable(nameBytes))
            {
                bssidDetails.ItsBroadcastName = Encoding.UTF8.GetString(nameBytes);
            }
        }

        private static void ParseArubaVendorElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            var ieData = informationElement.ItsData;
            var ouiType = ieData[3];

            if (ouiType != 1) return; // Aruba Type 1 is Device Name, we don't parse any other Vendor Specific Aruba Information Elements

            var nameLength = informationElement.ItsLength - VENDOR_ARUBA_NAME_OFFSET;
            var nameBytes = new byte[nameLength];

            Array.Copy(ieData, VENDOR_ARUBA_NAME_OFFSET, nameBytes, 0, nameLength);
            if (DeviceNameIsPrintable(nameBytes))
            {
                bssidDetails.ItsBroadcastName = Encoding.UTF8.GetString(nameBytes);
            }
        }

        private static void ParseCiscoDeviceNameElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            if (informationElement.ItsLength != 30) return;

            var ieData = informationElement.ItsData;

            var nameBytes = new byte[CISCO_NAME_LENGTH];
            Array.Copy(informationElement.ItsData, CISCO_NAME_OFFSET, nameBytes, 0, CISCO_NAME_LENGTH); 

            var deviceNameString = Encoding.UTF8.GetString(nameBytes);
            bssidDetails.ItsBroadcastName = deviceNameString.TrimEnd('\0');
        }

        private static void ParseVhtOperationElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            // Ignoring VHT in 2.4GHz
            if (informationElement.ItsLength != 5 || bssidDetails.ItsChannelInfo.ItsBand == ChannelBand.TwoGhz)
            {
                return;
            }

            var ieData = informationElement.ItsData;
            var channelWidthField = ieData[0];
            var seg0ChannelCenter = ieData[1];
            var seg1ChannelCenter = ieData[2];
            var channelInfo = bssidDetails.ItsChannelInfo;

            switch (channelWidthField)
            {
                case VHT_OP_CHAN_WIDTH_20_40:
                    if (seg0ChannelCenter >= 36)
                    {
                        channelInfo.ItsChannel = seg0ChannelCenter;
                        channelInfo.ItsChannelWidth =
                            ChannelInfo.GetAcChannelWidthByChannelName(channelInfo.ItsChannel);
                    }
                    // Dealing with Ubiquiti NOT setting the Center of the .11ac channel and only setting HT Operation details for 40 MHz channel
                    else if (seg0ChannelCenter == 0)
                    {
                        if (channelInfo.ItsChannelWidth == ChannelWidth.Forty &&
                            channelInfo.ItsHtSecondaryChannel > 0)
                        {
                            channelInfo.ItsChannel =
                                (channelInfo.ItsPrimaryChannel + channelInfo.ItsHtSecondaryChannel) / 2;
                        }
                    }
                    break;

                case VHT_OP_CHAN_WIDTH_80:
                    channelInfo.ItsChannelWidth = ChannelWidth.Eighty;
                    channelInfo.ItsChannel = seg0ChannelCenter;
                    break;

                case VHT_OP_CHAN_WIDTH_160:
                    channelInfo.ItsChannelWidth = ChannelWidth.OneSixty;
                    channelInfo.ItsChannel = seg0ChannelCenter;
                    break;

                case VHT_OP_CHAN_WIDTH_80_80:
                    channelInfo.ItsChannelWidth = ChannelWidth.EightyPlusEighty;
                    channelInfo.ItsChannel = seg0ChannelCenter;
                    channelInfo.ItsAcSecondaryChannel = seg1ChannelCenter;
                    break;
            }

            // Check Channel Center Frequency Segment 1 for 160MHz radio band
            if (seg1ChannelCenter > 0 && Math.Abs(seg1ChannelCenter - seg0ChannelCenter) == 8)
            {
                channelInfo.ItsChannelWidth = ChannelWidth.OneSixty;
                channelInfo.ItsChannel = seg1ChannelCenter;
            }
        }

        private static void ParseVhtCapabilitiesElement(IBaseDeviceDetails deviceDetails, InformationElement informationElement,
            ChannelBand channelBand)
        {
            // Note: Ignoring Channel Width in VHT Capabilities, only pulling the actual width from VHT Operation
            // Ignore further operations for 2.4Ghz
            if (informationElement.ItsLength != 12 || channelBand == ChannelBand.TwoGhz)
            {
                return;
            }
                
            var ieData = informationElement.ItsData;

            deviceDetails.ItsPhyTypeInfo.AddPhyType(PhyTypes.Ac);

            deviceDetails.ItsPhyTypeInfo.ItsVhtCapabilitiesInfo = BitConverter.ToUInt32(ieData, 0);

            // Given in the real world all streams always support the same MCS Map, no need to look at ALL of the streams
            var mcsMap = (uint) (ieData[4] & VHT_CAP_STREAM_1_MCS_MAP_MASK);
            if (mcsMap != VHT_NOT_SUPPORTED_STREAM_MCS_MAP)
            {
                deviceDetails.ItsMaxMcsIndex = Math.Max(mcsMap + 7, deviceDetails.ItsMaxMcsIndex);
            }

            ushort streamMap = (ushort) (ieData[5] << 8 | ieData[4]);
            ushort streamMask = 0x0003;
            uint spatialStreams;

            for (spatialStreams = 0; spatialStreams < 8; spatialStreams++)
            {
                if ((streamMap & streamMask) == VHT_NOT_SUPPORTED_STREAM_MCS_MAP)
                {
                    break;
                }

                streamMap = (ushort) (streamMap >> 2);
            }

            deviceDetails.ItsSpacialStreamCount = spatialStreams;
        }

        private static void ParseExtensionElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            switch (informationElement.ItsData[0])
            {
                case (int)InformationElementExtensionId.HeCapabilities:
                    ParseHeCapabilities(bssidDetails, informationElement);
                    break;

                case (int)InformationElementExtensionId.HeOperation:
                    break;
            }
        }

        private static void ParseRmCapabilitiesElement(IBaseDeviceDetails baseDeviceDetails, InformationElement informationElement)
        {
            baseDeviceDetails.ItsNeighborReportCapabilityFlag = (informationElement.ItsData[0] & 0x02) == 0x02 ;
        }

        private static void ParseHeCapabilities(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            // AP is WiFi 6
            bssidDetails.ItsPhyTypeInfo.AddPhyType(PhyTypes.Ax);
            var ieData = informationElement.ItsData;

            // Ignore HE MAC Capabilities info
            int offset = 7;

            // Parse HE Phy Capabilities info
            byte[] hePhyCapabilitiesInfoBytes = new byte[HE_PHY_CAPABILITIES_LENGTH];
            Array.Copy(ieData, offset, hePhyCapabilitiesInfoBytes, 0, HE_PHY_CAPABILITIES_LENGTH);

            offset += HE_PHY_CAPABILITIES_LENGTH;

            // Parse capablities info
            bssidDetails.ItsPhyTypeInfo.ItsHeCapabilitiesInfo = (ushort)(hePhyCapabilitiesInfoBytes[1] | hePhyCapabilitiesInfoBytes[2] << 8);

            // Parse suported MCS info
            var mcsMap = (uint)(ieData[offset] & HE_NOT_SUPPORTED_STREAM_MCS_MAP);
            if (mcsMap != HE_NOT_SUPPORTED_STREAM_MCS_MAP)
            {
                bssidDetails.ItsMaxMcsIndex = Math.Max(mcsMap * 2 + 7, bssidDetails.ItsMaxMcsIndex);
            }

            ushort supportedStreamMap = (ushort)(ieData[offset++] | ieData[offset++] << 8);
            uint spatialStreamsCount;

            // Search for the highest supported spatial stream
            for (spatialStreamsCount = 0; spatialStreamsCount < 8; spatialStreamsCount++)
            {
                if ((supportedStreamMap & HE_NOT_SUPPORTED_STREAM_MCS_MAP) == HE_NOT_SUPPORTED_STREAM_MCS_MAP)
                {
                    break;
                }

                supportedStreamMap = (ushort)(supportedStreamMap >> 2);
            }

            bssidDetails.ItsSpacialStreamCount = spatialStreamsCount;
        }

        private static void ParseHtInformationElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            if (informationElement.ItsLength != 22) return;

            bssidDetails.ItsPhyTypeInfo.AddPhyType(PhyTypes.N);

            var ieData = informationElement.ItsData;
            var primaryChannel = ieData[0];
            var secondaryChannelOffset = ieData[1] & 0x03;

            bssidDetails.ItsChannelInfo.ItsChannel = bssidDetails.ItsChannelInfo.ItsPrimaryChannel = primaryChannel;

            if (secondaryChannelOffset > 0)
            {
                bssidDetails.ItsChannelInfo.ItsHtSecondaryChannel =
                    primaryChannel + (uint) (secondaryChannelOffset == 3 && primaryChannel > 4 ? -4 : 4);
                bssidDetails.ItsChannelInfo.ItsChannelWidth = ChannelWidth.Forty;
            }
            else
            {
                bssidDetails.ItsChannelInfo.ItsChannelWidth = ChannelWidth.Twenty;
            }

            bssidDetails.ItsPhyTypeInfo.ItsHtNonGreenfieldPresentFlag = (ieData[2] & HT_INFO_NON_GREENFIELD_PRESENT_MASK) > 0;
        }

        private static void ParseHtCapabilitiesElement(IBaseDeviceDetails deviceDetails, InformationElement informationElement)
        {
            if (informationElement.ItsLength != 26) return;

            deviceDetails.ItsPhyTypeInfo.AddPhyType(PhyTypes.N);

            var ieData = informationElement.ItsData;
            deviceDetails.ItsPhyTypeInfo.ItsHtCapabilitiesInfo = (ushort)(ieData[1] << 8 | ieData[0]);

            // figure out number of spatial streams and max MCS Index - AC style
            var htSpatialStreams = 0u;
            for (var i = 0u; i < 4; i++)
            {
                if (ieData[3 + i] > 0)
                {
                    htSpatialStreams = i + 1;
                }
            }

            // Don't override VHT capabilities
            deviceDetails.ItsSpacialStreamCount = Math.Max(deviceDetails.ItsSpacialStreamCount, htSpatialStreams);

            var htMaxMcsIndex = 0u;
            for (var i = 0; i < 8; i++)
            {
                if ((ieData[3] >> i) > 0)
                {
                    htMaxMcsIndex = (uint)i;
                }
            }

            // Don't override VHT capabilities
            deviceDetails.ItsMaxMcsIndex = Math.Max(deviceDetails.ItsMaxMcsIndex, htMaxMcsIndex);
        }

        private static void ParseExtendedCapabilitiesElement(IBaseDeviceDetails deviceDetails, InformationElement informationElement)
        {
            if (informationElement.ItsLength != 8) return;

            // switch from network order to little endian order before converter to ulong
            Array.Reverse(informationElement.ItsData);
            deviceDetails.ItsPhyTypeInfo.ItsExtendedCapabilitiesInfo = BitConverter.ToUInt64(informationElement.ItsData, 0);
            deviceDetails.ItsSupportsBssTransitionFlag = (deviceDetails.ItsPhyTypeInfo.ItsExtendedCapabilitiesInfo & EXT_CAP_BSS_TRANS_FLAG_MASK) > 0;
        }

        private static void ParseBssLoadElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            if (informationElement.ItsLength != 5) return;

            var data = informationElement.ItsData;

            bssidDetails.ItsBssClientCount = (int)(data[1] << 8) + data[0];
            bssidDetails.ItsChannelInfo.ItsBssRawChannelUtilization = data[2] / 255d;
        }

        private static void ParseCountryInformationElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            if (informationElement.ItsLength < 6) return;

            // There is a third ASCII byte that is either:
            // I - Indoor
            // O = Outdoor
            // (space) = Both
            // followed by optional list of transmit power per channel
            bssidDetails.ItsCountryCode = Encoding.ASCII.GetString(informationElement.ItsData, 0, 2);
        }

        private static void ParseExtendedRatesElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            bssidDetails.ItsPhyTypeInfo.AddPhyType(PhyTypes.G);

            var rateBytes = informationElement.ItsData;

            foreach (var rateByte in rateBytes)
            {
                var rate = rateByte / 2.0;
                bssidDetails.ItsMaxDataRate = Math.Max(bssidDetails.ItsMaxDataRate, rate);
            }
        }

        private static void ParseErpElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            if (informationElement.ItsLength != 1) return;

            bssidDetails.ItsPhyTypeInfo.AddPhyType(PhyTypes.G);
            var erpData = informationElement.ItsData[0];
            bssidDetails.ItsPhyTypeInfo.ItsNonErpPresentFlag = (erpData & ERP_NON_ERP_PRESENT_MASK) != 0;
            bssidDetails.ItsPhyTypeInfo.ItsProtectionEnabledFlag = (erpData & ERP_USE_PROTECTION_MASK) != 0;
        }

        private static void ParseSupportedRatesElement(IBaseDeviceDetails deviceDetails, InformationElement informationElement)
        {
            var rateBytes = informationElement.ItsData;

            foreach (var rateByte in rateBytes)
            {
                var rateIsBasic = (rateByte & BSS_BASIC_RATE_SET_MASK) != 0;
                var cleanRateByte = rateByte & BSS_SUPPORTED_RATE_MASK;
                var rate = cleanRateByte / 2.0;

                if (rateIsBasic)
                {
                    if (!deviceDetails.ItsBasicRates.Contains(rate))
                    {
                        deviceDetails.ItsBasicRates.Add(rate);
                    }
                }

                deviceDetails.ItsMaxDataRate = Math.Max(deviceDetails.ItsMaxDataRate, rate);

                if (rate < 6 || rate == 11)
                {
                    deviceDetails.ItsPhyTypeInfo.AddPhyType(PhyTypes.B);
                }
            }
        }

        private static void ParseRobustSecurityElement(IBaseDeviceDetails baseDeviceDetails, InformationElement informationElement)
        {
            const int minimumRequiredLength = 8;//version = 2, Group Cipher Suite = 4,  Pairwise count=2
            if (informationElement.ItsLength < minimumRequiredLength)
                return;

            var data = informationElement.ItsData;
            baseDeviceDetails.ItsProtectedManagementFramesCapabilityFlag = ParseProtectedManagementFrames(data, 0);

            var akmTypes = ParseAuthenticationKeyManagement(data, 0);
            baseDeviceDetails.ItsFastRoamingCapabilityFlag = HasFastRoamingCapability(akmTypes);

            var securityInfo = baseDeviceDetails.ItsSecurityInfo;
            securityInfo.ItsAuthenticationKeyManagementTypes |= akmTypes;

            if ((akmTypes & (AuthenticationKeyManagementTypes.ENTERPRISE | AuthenticationKeyManagementTypes.FT_ENTERPRISE)) > 0)
            {
                securityInfo.ItsAuthentication |= AuthenticationTypes.WPA2_ENTERPRISE;
            }
            if ((akmTypes & (AuthenticationKeyManagementTypes.PERSONAL | AuthenticationKeyManagementTypes.FT_PERSONAL)) > 0)
            {
                securityInfo.ItsAuthentication |= AuthenticationTypes.WPA2_PRE_SHARED_KEY;
            }
            if ((akmTypes & (AuthenticationKeyManagementTypes.SAE_ENTERPRISE)) > 0)
            {
                securityInfo.ItsAuthentication |= AuthenticationTypes.WPA3_ENTERPRISE;
            }
            if ((akmTypes & (AuthenticationKeyManagementTypes.SAE_PERSONAL)) > 0)
            {
                securityInfo.ItsAuthentication |= AuthenticationTypes.WPA3_PRE_SHARED_KEY;
            }
        }

        private static bool HasFastRoamingCapability(AuthenticationKeyManagementTypes akmTypes)
        {
            bool fastRoamingFlag = false;

            fastRoamingFlag |= (akmTypes & AuthenticationKeyManagementTypes.FT_PERSONAL) > 0;
            fastRoamingFlag |= (akmTypes & AuthenticationKeyManagementTypes.FT_SAE_PERSONAL) > 0;
            fastRoamingFlag |= (akmTypes & AuthenticationKeyManagementTypes.FT_ENTERPRISE) > 0;
            fastRoamingFlag |= (akmTypes & AuthenticationKeyManagementTypes.FT_SAE_ENTERPRISE) > 0;

            return fastRoamingFlag;
        }

        private static void ParseWepWpaElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            const int MIN_REQ_LENGTH = 12;
            const int WPA_HEADER_LENGTH = 4;
            const int WPS_CONFIGURATION_INDEX = 13;

            const int WPA_TYPE = 0x01;
            const int WEP_TYPE = 0x02;
            const int WPS_TYPE = 0x04;

            if (informationElement.ItsLength < MIN_REQ_LENGTH) return;
            var data = informationElement.ItsData;
            var securityInfo = bssidDetails.ItsSecurityInfo;

            switch (data[3])
            {
                //case WEP_TYPE:
                //    securityInfo.ItsAuthentication |= AuthenticationTypes.WEP;
                //    break;

                //case WPS_TYPE:
                //    // check for WPS configured
                //    if (informationElement.ItsLength > WPS_CONFIGURATION_INDEX)
                //    {
                //        if (data[WPS_CONFIGURATION_INDEX] > 0)
                //        {
                //            securityInfo.ItsWpsFlag = true;
                //        }
                //    }
                //    break;

                case WPA_TYPE:
                    // call ParseAuthenticationKeyManagement and then set authentication to WPA types...
                    var akmTypes = ParseAuthenticationKeyManagement(data, WPA_HEADER_LENGTH);
                    securityInfo.ItsAuthenticationKeyManagementTypes |= akmTypes;
                    if ((akmTypes & (AuthenticationKeyManagementTypes.ENTERPRISE |
                                     AuthenticationKeyManagementTypes.FT_ENTERPRISE)) > 0)
                    {
                        securityInfo.ItsAuthentication |= AuthenticationTypes.WPA_ENTERPRISE;
                    }

                    if ((akmTypes & (AuthenticationKeyManagementTypes.PERSONAL |
                                     AuthenticationKeyManagementTypes.FT_PERSONAL)) > 0)
                    {
                        securityInfo.ItsAuthentication |= AuthenticationTypes.WPA_PRE_SHARED_KEY;
                    }
                    if ((akmTypes & (AuthenticationKeyManagementTypes.SAE_ENTERPRISE)) > 0)
                    {
                        securityInfo.ItsAuthentication |= AuthenticationTypes.WPA3_ENTERPRISE;
                    }
                    if ((akmTypes & (AuthenticationKeyManagementTypes.SAE_PERSONAL)) > 0)
                    {
                        securityInfo.ItsAuthentication |= AuthenticationTypes.WPA3_PRE_SHARED_KEY;
                    }

                    break;
            }
        }

        private static bool ParseProtectedManagementFrames(byte[] data, int offset)
        {
            const int RNS_BYTES_TO_IGNORE = 6;
            const int SUITE_COUNT_FIELD_SIZE = 2;
            const int CAPABILITIES_FIELD_SIZE = 2;
            const int SUITE_FIELD_SIZE = 4;

            offset += RNS_BYTES_TO_IGNORE;
            var pairwiseCipherCount = data[offset++] + (data[offset++] << 8);
            // skip passed pairwise cipher suites too
            offset += pairwiseCipherCount * SUITE_FIELD_SIZE;

            if (offset <= data.Length - SUITE_COUNT_FIELD_SIZE)
            {
                var akmCipherCount = data[offset++] + (data[offset++] << 8);

                // skip Auth Key Management Suite Count 
                offset += akmCipherCount * SUITE_FIELD_SIZE;

                if(offset <= data.Length - CAPABILITIES_FIELD_SIZE)
                {
                    var rsnCapabilitiesInfo = data[offset++] + (data[offset++] << 8);

                    return (rsnCapabilitiesInfo & 0x80) == 0x80;
                }
            }
            
            return false;
        }

        private static AuthenticationKeyManagementTypes ParseAuthenticationKeyManagement(byte[] data, int offset)
        {
            var aggregateAkmTypes = AuthenticationKeyManagementTypes.UNKNOWN;

            try
            {
                const int AKM_OUI_SIZE = 3;
                // Ignore Version and Group Cipher Suite. Version is 2 bytes and Group Cipher Suite is 4 bytes
                const int RNS_BYTES_TO_IGNORE = 6;
                const int SUITE_COUNT_FIELD_SIZE = 2;
                const int SUITE_FIELD_SIZE = 4;

                offset += RNS_BYTES_TO_IGNORE;
                var pairwiseCipherCount = data[offset++] + (data[offset++] << 8);
                // skip passed pairwise cipher suites too
                offset += pairwiseCipherCount * SUITE_FIELD_SIZE;

                if (offset <= data.Length - SUITE_COUNT_FIELD_SIZE)
                {

                    var akmCipherCount = data[offset++] + (data[offset++] << 8);
                    for (var i = 0; i < akmCipherCount; i++)
                    {
                        offset += AKM_OUI_SIZE;
                        if (offset >= data.Length) break;
                        var akmType = data[offset++];
                        switch (akmType)
                        {
                            case AkmBroadcastType.Enterprise:
                                aggregateAkmTypes |= AuthenticationKeyManagementTypes.ENTERPRISE;
                                break;

                            case AkmBroadcastType.FtEnterprise:
                                aggregateAkmTypes |= AuthenticationKeyManagementTypes.FT_ENTERPRISE;
                                break;

                            case AkmBroadcastType.Personal:
                                aggregateAkmTypes |= AuthenticationKeyManagementTypes.PERSONAL;
                                break;

                            case AkmBroadcastType.FtPersonal:
                                aggregateAkmTypes |= AuthenticationKeyManagementTypes.FT_PERSONAL;
                                break;

                            case AkmBroadcastType.SaePersonal:
                                aggregateAkmTypes |= AuthenticationKeyManagementTypes.SAE_PERSONAL;
                                break;
                            
                            case AkmBroadcastType.SaeEnterprise:
                                aggregateAkmTypes |= AuthenticationKeyManagementTypes.SAE_ENTERPRISE;
                                break;

                            case AkmBroadcastType.FtSaePersonal:
                                aggregateAkmTypes |= AuthenticationKeyManagementTypes.FT_SAE_PERSONAL;
                                break;

                            case AkmBroadcastType.FtSaeEnterprise:
                                aggregateAkmTypes |= AuthenticationKeyManagementTypes.FT_SAE_ENTERPRISE;
                                break;
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                //The IE was malformed in some way. Has been seen on XP 
                Trace.TraceError("Index out of range exception when trying to parse RSN information element.\n{0}",
                    e.Message);
            }

            return aggregateAkmTypes;
        }

        private static void ParseDsParameterElement(IBssidDetails bssidDetails, InformationElement informationElement)
        {
            if (informationElement.ItsLength != 1) return;
            bssidDetails.ItsChannelInfo.ItsChannel = informationElement.ItsData[0];
            if (bssidDetails.ItsChannelInfo.ItsChannel >= 14)
            {
                bssidDetails.ItsPhyTypeInfo.AddPhyType(PhyTypes.A);
            }
        }

        private static bool DeviceNameIsPrintable(IEnumerable<byte> deviceNameBytes)
        {
            return deviceNameBytes.All(character => ((character & 0x80) == 0) && ((character & 0x60) != 0));
        }

        private static List<InformationElement> BuildInformationElements(byte[] packetBytes)
        {
            
            var informationElements = new List<InformationElement>();
            var index = 0;

            // we index two bytes passed index into the packetBytes array so ensure packetBytes is long enough
            while (index < packetBytes.Length - 2)
            {
                var ie = new InformationElement { ItsId = (InformationElementId)packetBytes[index++], ItsLength = packetBytes[index++] };

                if (packetBytes.Length - index < ie.ItsLength) break;

                ie.ItsData = new byte[ie.ItsLength];
                Array.Copy(packetBytes, index, ie.ItsData, 0, ie.ItsLength);

                informationElements.Add(ie);
                index += ie.ItsLength;
            }
            return informationElements;
        }

        // Derive missing information 
        private static void PostProcessBeaconInformation(IBssidDetails bssidDetails)
        {
            // Fix the WEP/Open Security 
            var securityInfo = bssidDetails.ItsSecurityInfo;

            if ((bssidDetails.ItsCapabilitiesInformation & WLAN_FIXED_CPABILITIES_PRIVACY_BITMASK) == 0x0000)
            {
                securityInfo.ItsAuthentication |= AuthenticationTypes.OPEN;
            }
            else if ((bssidDetails.ItsCapabilitiesInformation & WLAN_FIXED_CPABILITIES_PRIVACY_BITMASK) == WLAN_FIXED_CPABILITIES_PRIVACY_BITMASK && !bssidDetails.ItsHasRSNElement)
            {
                securityInfo.ItsAuthentication |= AuthenticationTypes.WEP;
            }

            // Calculate max data rate for .11ax
            if ((bssidDetails.ItsPhyTypeInfo.ItsPhyTypesEnum & (uint)PhyTypes.Ax) == (uint)PhyTypes.Ax)
            {
                var chanWidthIndexOffset = 0;

                bool isLgi = (bssidDetails.ItsPhyTypeInfo.ItsHeCapabilitiesInfo & HE_CAP_3_2_GI_MASK) > 0;
                bool isSgi = (bssidDetails.ItsPhyTypeInfo.ItsHeCapabilitiesInfo & HE_CAP_0_8_GI_MASK) > 0;

                switch (bssidDetails.ItsChannelInfo.ItsChannelWidth)
                {
                    case ChannelWidth.Twenty:
                        chanWidthIndexOffset = 0;
                        break;

                    case ChannelWidth.Forty:
                        chanWidthIndexOffset = 96;
                        break;

                    case ChannelWidth.Eighty:
                        chanWidthIndexOffset = 192;
                        break;

                    case ChannelWidth.EightyPlusEighty:
                    case ChannelWidth.OneSixty:
                        chanWidthIndexOffset = 288;
                        break;
                }

                var ssIndexOffset = 0u;
                if (bssidDetails.ItsSpacialStreamCount != 0)
                {
                    ssIndexOffset = (bssidDetails.ItsSpacialStreamCount - 1) * 12;
                }

                var rateIndex = chanWidthIndexOffset + ssIndexOffset + bssidDetails.ItsMaxMcsIndex;

                if (isSgi)
                {
                    bssidDetails.ItsMaxDataRate = HE_DATA_RATE_SGI_LOOKUP_TABLE[rateIndex];
                }
                else if (isLgi)
                {
                    bssidDetails.ItsMaxDataRate = HE_DATA_RATE_LGI_LOOKUP_TABLE[rateIndex];
                }
                else
                {
                    bssidDetails.ItsMaxDataRate = HE_DATA_RATE_MGI_LOOKUP_TABLE[rateIndex];
                }

                if (bssidDetails.ItsPhyTypeInfo.ItsPhyTypesEnum == (uint)PhyTypes.B)
                {
                    bssidDetails.ItsChannelInfo.ItsBOnlyFlag = true;
                }
            }
            // Calculate max data rate for .11n or .11ac
            else if (bssidDetails.ItsPhyTypeInfo.ItsHtCapabilitiesInfo > 0 ||
                bssidDetails.ItsPhyTypeInfo.ItsVhtCapabilitiesInfo > 0)
            {
                var chanWidthIndexOffset = 0;
                var isSgi = false;
                switch (bssidDetails.ItsChannelInfo.ItsChannelWidth)
                {
                    case ChannelWidth.Twenty:
                        isSgi = (bssidDetails.ItsPhyTypeInfo.ItsHtCapabilitiesInfo & HT_CAP_SHORT_GI_20_MASK) > 0;
                        chanWidthIndexOffset = 0;
                        break;

                    case ChannelWidth.Forty:
                        isSgi = (bssidDetails.ItsPhyTypeInfo.ItsHtCapabilitiesInfo & HT_CAP_SHORT_GI_40_MASK) > 0;
                        chanWidthIndexOffset = 40;
                        break;

                    case ChannelWidth.Eighty:
                        isSgi = (bssidDetails.ItsPhyTypeInfo.ItsVhtCapabilitiesInfo & VHT_CAP_SHORT_GI_80_MASK) > 0;
                        chanWidthIndexOffset = 80;
                        break;

                    case ChannelWidth.EightyPlusEighty:
                    case ChannelWidth.OneSixty:
                        isSgi = (bssidDetails.ItsPhyTypeInfo.ItsVhtCapabilitiesInfo & VHT_CAP_SHORT_GI_160_MASK) > 0;
                        chanWidthIndexOffset = 120;
                        break;
                }

                var ssIndexOffset = 0u;
                if (bssidDetails.ItsSpacialStreamCount != 0)
                {
                    ssIndexOffset = (bssidDetails.ItsSpacialStreamCount - 1) * 10;
                }

                var rateIndex = chanWidthIndexOffset + ssIndexOffset + bssidDetails.ItsMaxMcsIndex;

                bssidDetails.ItsMaxDataRate = isSgi ? DATA_RATE_SGI_LOOKUP_TABLE[rateIndex] : DATA_RATE_LGI_LOOKUP_TABLE[rateIndex];

                if (bssidDetails.ItsPhyTypeInfo.ItsPhyTypesEnum == (uint)PhyTypes.B)
                {
                    bssidDetails.ItsChannelInfo.ItsBOnlyFlag = true;
                }
            }
        }

        #endregion Methods
    }
}
