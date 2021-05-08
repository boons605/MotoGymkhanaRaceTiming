using System;
using System.Collections.Generic;
using System.Text;

namespace RaceManagement
{
    public abstract class ManualEventArgs : EventArgs
    {
        /// <summary>
        /// Name of the official that issued the event
        /// </summary>
        public readonly string StaffName;

        public readonly DateTime Received;
        
        /// <summary>
        /// The rider this event applies to
        /// </summary>
        public readonly string RiderName;

        public ManualEventArgs(DateTime received, string riderName, string staffName)
        {
            Received = received;
            StaffName = staffName;
            RiderName = riderName;
        }
    }

    public class ManualDNFEventArgs : ManualEventArgs
    {
        public ManualDNFEventArgs(DateTime received, string riderName, string staffName) 
            : base(received, riderName, staffName)
        {
        }
    }

    /// <summary>
    /// Event when a race official disqualifies a lap
    /// Can be issued while the rider is on track as well as after a rider is finished
    /// </summary>
    public class DSQEventArgs : ManualEventArgs
    {
        public DSQEventArgs(DateTime received, string riderName, string staffName) 
            : base(received, riderName, staffName)
        {
        }
    }

    public class PenaltyEventArgs : ManualEventArgs
    {
        public readonly string Reason;
        public readonly int Seconds;

        public PenaltyEventArgs(DateTime received, string riderName, string staffName, string reason, int seconds) : base(received, riderName, staffName)
        {
            Reason = reason;
            Seconds = seconds;
        }
    }
}
