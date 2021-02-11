// <copyright file="IRiderIdUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
using System;
using System.Collections.Generic;

namespace SensorUnits.RiderIdUnit
{
    using System;
    using System.Collections.Generic;
    using Models;

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

        string UnitId { get; }

        /// <summary>
        /// A unit will store which riders it should report about, to avoid reporting riders or loose senders that are near the sensor range
        /// </summary>
        void ClearKnownRiders();

        /// <summary>
        /// Add new riders that the unit should report about.
        /// </summary>
        /// <param name="riders">The new riders, Guid should match what the sensor receives. Both sensorId and name should be unknown to the unit</param>
        void AddKnownRiders(List<Rider> riders);

        /// <summary>
        /// Avoid further events for this rider. To resume events for this rider they should be added again
        /// </summary>
        /// <param name="name">the name of the rider as reported by this unit in the exposed events</param>
        void RemoveKnownRider(string name);
    }
}
