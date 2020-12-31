using System;
using System.Collections.Generic;

namespace RiderIdUnit
{
    /// <summary>
    /// A rider id unit will identify riders that are near its sensor. It cannot de precise timing, so an id unit will not report times with its events
    /// </summary>
    public interface IRiderIdUnit
    {
        /// <summary>
        /// This event will fire when a rider enters the units sensor range
        /// </summary>
        event EventHandler<RiderIdEventArgs> OnRiderId;

        /// <summary>
        /// This event will fire when a rider leaves sensor range
        /// </summary>
        event EventHandler<RiderIdEventArgs> OnRiderExit;

        /// <summary>
        /// A unit will store which riders it should report about, to avoid reporting riders or loose senders that are near the sensor range
        /// </summary>
        void ClearKnownRiders();

        /// <summary>
        /// Add new riders that the unit should report about.
        /// </summary>
        /// <param name="riders">The new riders, Guid should match what the sensor receives. Both sensorId and name should be unkown to the unit</param>
        void AddKnownRiders(List<(byte[] sensorId, string name)> riders);

        /// <summary>
        /// Avoid further events for this rider. To resume events for this rider they should be added again
        /// </summary>
        /// <param name="name"></param>
        void RemoveKnownRider(string name);
    }

    public class RiderIdEventArgs : EventArgs
    {
        /// <summary>
        /// Name associated with the received sensor id
        /// </summary>
        public readonly string RiderName;
        /// <summary>
        /// Date reported by the sensor
        /// </summary>
        public readonly byte[] SensorId;
        /// <summary>
        /// Date and time when this message was received
        /// </summary>
        public DateTime Received;
        /// <summary>
        /// idebtifier for the unit that throws the event
        /// </summary>
        public readonly string UnitId;
    }
}
