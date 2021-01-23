using System;
using System.Collections.Generic;
using System.Text;

namespace RiderIdUnit
{
    public class RiderIDQueuedEvent
    {
        public enum RiderIdQueuedEventType
        {
            Entered,

            Exit
        }

        public readonly RiderIdEventArgs EventArgs;

        public readonly RiderIdQueuedEventType Type;

        public RiderIDQueuedEvent(RiderIdEventArgs args, RiderIdQueuedEventType eventType)
        {
            this.EventArgs = args;
            this.Type = eventType;
        }
    }
}
