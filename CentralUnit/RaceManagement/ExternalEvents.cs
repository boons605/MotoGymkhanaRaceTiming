using Newtonsoft.Json;
using System;

namespace RaceManagement
{
    public abstract class ManualEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the official that issued the event
        /// </summary>
        [JsonRequired]
        public readonly string StaffName;

        public readonly DateTime Received;

        /// <summary>
        /// The rider this event applies to
        /// </summary>
        [JsonRequired]
        public readonly Guid RiderId;

        public ManualEventArgs(DateTime received, Guid riderId, string staffName)
        {
            Received = received;
            StaffName = staffName;
            RiderId = riderId;
        }
    }

    public class ManualDNFEventArgs : ManualEventArgs
    {
        public ManualDNFEventArgs(DateTime received, Guid riderId, string staffName) 
            : base(received, riderId, staffName)
        {
        }
    }

    /// <summary>
    /// Event when a race official disqualifies a lap
    /// Can be issued while the rider is on track as well as after a rider is finished
    /// </summary>
    public class DSQEventArgs : ManualEventArgs
    {
        [JsonRequired]
        public readonly string Reason;

        public DSQEventArgs(DateTime received, Guid riderId, string staffName, string reason) 
            : base(received, riderId, staffName)
        {
            Reason = reason;
        }
    }

    public class PenaltyEventArgs : ManualEventArgs
    {
        [JsonRequired]
        public readonly string Reason;

        [JsonRequired]
        public readonly int Seconds;

        public PenaltyEventArgs(DateTime received, Guid riderId, string staffName, string reason, int seconds) : base(received, riderId, staffName)
        {
            Reason = reason;
            Seconds = seconds;
        }
    }

    public class RiderReadyEventArgs : ManualEventArgs
    {
        public RiderReadyEventArgs(DateTime received, Guid riderId, string staffName) : base(received, riderId, staffName)
        {
        }
    }

    public class RiderFinishedEventArgs : ManualEventArgs
    {
        /// <summary>
        /// The id of the timing event that should be matched to the rider
        /// </summary>
        public readonly Guid TimeId;

        public RiderFinishedEventArgs(DateTime received, Guid riderId, string staffName, Guid timeId) : base(received, riderId, staffName)
        {
            TimeId = timeId;
        }
    }

}
