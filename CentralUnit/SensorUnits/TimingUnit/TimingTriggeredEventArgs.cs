// <copyright file="TimingTriggeredEventArgs.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace SensorUnits.TimingUnit
{
    using System;

    /// <summary>
    /// Event that happens when a timing gate is triggered. A single unit may have multiple gates. Hence that GateId field
    /// </summary>
    public class TimingTriggeredEventArgs : EventArgs
    {
        /// <summary>
        /// Milliseconds since units was switched on when sensor was triggered
        /// </summary>
        public readonly long Microseconds;

        /// <summary>
        /// Identifier for timing unit
        /// </summary>
        public readonly string UnitId;

        /// <summary>
        /// Which gate the sensor was triggered for (start or stop gate for example)
        /// </summary>
        public readonly int GateId;

        /// <summary>
        /// Time when the event was received on this machine. DO NOT USE FOR LAP TIMES
        /// </summary>
        public readonly DateTime Received;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimingTriggeredEventArgs" /> class.
        /// </summary>
        /// <param name="microseconds">The timestamp (in the timer domain) from the timer.</param>
        /// <param name="gateId">The gate ID for the timer</param>
        public TimingTriggeredEventArgs(long microseconds, int gateId) : this(microseconds, "timerUnit", gateId, DateTime.Now)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimingTriggeredEventArgs" /> class.
        /// </summary>
        /// <param name="microseconds">The timestamp (in the timer domain) from the timer.</param>
        /// <param name="unitId">The timer unit ID</param>
        /// <param name="gateId">The gate ID for the timer</param>
        /// <param name="received">The date/time this event was received</param>
        public TimingTriggeredEventArgs(long microseconds, string unitId, int gateId, DateTime received)
        {
            this.Microseconds = microseconds;
            this.UnitId = unitId;
            this.GateId = gateId;
            this.Received = received;
        }
    }
}
