using System;

namespace MetaGeek.Tonic.Common.Events
{
    public class CaptureSelectorTimeRangeChangedEventArgs : EventArgs
    {
        public DateTime ItsStartTime { get; }
        public DateTime ItsEndTime { get; }

        public CaptureSelectorTimeRangeChangedEventArgs(DateTime startDateTime, DateTime endDateTime)
        {
            ItsStartTime = startDateTime;
            ItsEndTime = endDateTime;
        }
    }
}
