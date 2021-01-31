// <copyright file="ITimingUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using System;

namespace SensorUnits.TimingUnit
{
    /// <summary>
    /// A timing unit will send an event every time a rider crosses its sensors. It cannot identify riders so it will only report that 'something' crossed its sensor
    /// </summary>
    public interface ITimingUnit
    {
        /// <summary>
        /// This event will fire when the sensor of the unit is triggered.
        /// </summary>
        event EventHandler<TimingTriggeredEventArgs> OnTrigger;
    }

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

        public TimingTriggeredEventArgs(long microseconds, string unitId, int gateId, DateTime received)
        {
            Microseconds = microseconds;
            UnitId = unitId;
            GateId = gateId;
            Received = received;
        }
    }
}
