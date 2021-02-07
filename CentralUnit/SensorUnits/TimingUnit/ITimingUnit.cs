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
}
