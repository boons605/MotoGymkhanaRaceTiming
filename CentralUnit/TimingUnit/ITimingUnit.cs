using System;

namespace TimingUnit
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

        public DateTime Received;
    }
}
