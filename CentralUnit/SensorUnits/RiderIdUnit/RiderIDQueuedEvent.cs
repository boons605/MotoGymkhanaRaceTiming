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
        /// The type of event.
        /// </summary>
        public readonly RiderIdQueuedEventType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="RiderIDQueuedEvent" /> class.
        /// </summary>
        /// <param name="args">Event data for this event.</param>
        /// <param name="eventType">The type of event</param>
        public RiderIDQueuedEvent(RiderIdEventArgs args, RiderIdQueuedEventType eventType)
        {
            this.EventArgs = args;
            this.Type = eventType;
        }

        /// <summary>
        /// Enum to identify the event type.
        /// </summary>
        public enum RiderIdQueuedEventType
        {
            /// <summary>
            /// The event indicates a rider has entered the detection range.
            /// </summary>
            Entered,

            /// <summary>
            /// The event indicates a rider has left the detection range.
            /// </summary>
            Exit
        }
    }
}
