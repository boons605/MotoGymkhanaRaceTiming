// <copyright file="RiderIDQueuedEvent.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace SensorUnits.RiderIdUnit
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class RiderIDQueuedEvent
    {
        /// <summary>
        /// The event data.
        /// </summary>
        public readonly RiderIdEventArgs EventArgs;

        /// <summary>
        /// Initializes a new instance of the <see cref="RiderIDQueuedEvent" /> class.
        /// </summary>
        /// <param name="args">Event data for this event.</param>
        /// <param name="eventType">The type of event</param>
        public RiderIDQueuedEvent(RiderIdEventArgs args)
        {
            this.EventArgs = args;
        }
    }
}
