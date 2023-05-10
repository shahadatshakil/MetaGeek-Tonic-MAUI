using System;
using System.IO;

namespace MetaGeek.Tonic.Common.Resources
{
    public static class CaptureMagicStrings
    {
        public static class CaptureFilePath
        {
            public static string DefaultCaptureSavedRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Tonic Captures");
            public static string SavedPcapFolderPath = Path.Combine(DefaultCaptureSavedRootPath, "Tonic Temporary PCAPs");
            public static string TemporaryWsxFolderPath = Path.Combine(DefaultCaptureSavedRootPath, "Tonic Temporary WSXs");
            public static string TemporaryExtractedFilePath = Path.Combine(DefaultCaptureSavedRootPath, "Temporary Extracted Files");
        }
    }
}
