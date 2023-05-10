using System;

namespace MetaGeek.Tonic.Common.Events
{
    public class CaptureFileParseRequestEventArgs : EventArgs
    {
        public bool ItsZippedFileFlag { get; set; }
        public string ItsPcapFilePath { get; set; }
        public string ItsWsxFilePath { get; set; }
        public string ItsZippedFilePath { get; set; }

        public CaptureFileParseRequestEventArgs(string pcapFilePath, string wsxFilePath = null, string zippedFilePath = null, bool zippedFileFlag = false)
        {
            ItsZippedFileFlag = zippedFileFlag;
            ItsPcapFilePath = pcapFilePath;
            ItsWsxFilePath = wsxFilePath;
            ItsZippedFilePath = zippedFilePath;
        }
    }
}
