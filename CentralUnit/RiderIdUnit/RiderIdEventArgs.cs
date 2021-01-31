// <copyright file="RiderIdEventArgs.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

namespace RiderIdUnit
{
    using System;
    using Models;

    /// <summary>
    /// Event that is triggered when a rider is picked up by an id unit
    /// </summary>
    public class RiderIdEventArgs : EventArgs
    {
        /// <summary>
        /// Rider associated with the received sensor id
        /// </summary>
        public readonly Rider Rider;

        /// <summary>
        /// identifier for the unit that throws the event
        /// </summary>
        public readonly string UnitId;

        /// <summary>
        /// Date and time when this message was received
        /// </summary>
        public DateTime Received;

        public RiderIdEventArgs(Rider rider, DateTime received, string unitId)
        {
            Rider = rider;
            Received = received;
            UnitId = unitId;
        }
    }
}
