// <copyright file="RaceEvents.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using System;

namespace Models
{
    /// <summary>
    /// Base class for representing things that happened during a race. Every event has three basic properties: event id, who the event applies to and when the event was recorded
    /// </summary>
    public class RaceEvent
    {
        /// <summary>
        /// The moment the event was processed
        /// </summary>
        public DateTime Time;

        /// <summary>
        /// Unique id to refer to this event
        /// </summary>
        public Guid EventId;

        /// <summary>
        /// The rider this event happened to
        /// </summary>
        public Rider Rider { get; protected set; }

        public RaceEvent(DateTime time, Rider rider, Guid eventId)
        {
            Time = time;
            Rider = rider;
            EventId = eventId;
        }
    }

    /// <summary>
    /// Event to mark when a rider has finished. A rider is finished when we have recorded 4 essential events: id at start box, timing at start box, id at end box and timing at end box
    /// </summary>
    public class FinishedEvent : RaceEvent
    {
        /// <summary>
        /// Lap time in microseconds
        /// </summary>
        public long LapTime => TimeEnd.Microseconds - TimeStart.Microseconds;

        /// <summary>
        /// The event where the id unit at the start box picks up the rider
        /// </summary>
        public readonly EnteredEvent Entered;

        /// <summary>
        /// The event where the start timing gate is triggered by the rider
        /// </summary>
        public readonly TimingEvent TimeStart;

        /// <summary>
        /// The event where the end timing gate is triggered by the rider
        /// </summary>
        public readonly TimingEvent TimeEnd;

        /// <summary>
        /// The event where the id unit at the stop box picks up the rider
        /// </summary>
        public readonly LeftEvent Left;

        public FinishedEvent(EnteredEvent entered, TimingEvent timeStart, TimingEvent timeEnd, LeftEvent left)
            : base(timeEnd.Time, timeEnd.Rider, Guid.NewGuid())
        {
            Entered = entered;
            TimeStart = timeStart;
            TimeEnd = timeEnd;
            Left = left;
        }
    }

    /// <summary>
    /// Event when the rider id is picked up at the start gate
    /// </summary>
    public class EnteredEvent : RaceEvent
    {
        public EnteredEvent(DateTime time, Rider rider)
            : base(time, rider, Guid.NewGuid()) { }
    }

    /// <summary>
    /// Event when the rider id is picked up at the end gate
    /// </summary>
    public class LeftEvent : RaceEvent
    {
        public LeftEvent(DateTime time, Rider rider)
         : base(time, rider, Guid.NewGuid()) { }
    }

    /// <summary>
    /// Event when a rider triggers a timing gate
    /// </summary>
    public class TimingEvent : RaceEvent
    {
        /// <summary>
        /// Microseconds reported by the timer
        /// </summary>
        public readonly long Microseconds;

        /// <summary>
        /// Which timing gate this event happened for
        /// </summary>
        public readonly int GateId;

        public TimingEvent(DateTime time, Rider rider, long microseconds, int gateId) : base(time, rider, Guid.NewGuid())
        {
            Microseconds = microseconds;
            GateId = gateId;
        }

        /// <summary>
        /// For the end timing gate we may receive a timing event before a rider id
        /// So we might have to set this field after we've matched it
        /// </summary>
        /// <param name="rider"></param>
        public void SetRider(Rider rider)
        {
            Rider = rider;
        }
    }

    /// <summary>
    /// Event when a rider does not finish their lap. This is detected when a rider that started after them finished earlier
    /// </summary>
    public class DNFEvent : RaceEvent
    {
        /// <summary>
        /// A DNF happens when a driver who started later finished before this driver did
        /// </summary>
        public readonly FinishedEvent OtherRider;

        /// <summary>
        /// The event where this driver was picked up at the start gate
        /// </summary>
        public readonly EnteredEvent ThisRider;

        public DNFEvent(FinishedEvent otherRider, EnteredEvent thisRider)
            : base(otherRider.Time, thisRider.Rider, Guid.NewGuid())
        {
            OtherRider = otherRider;
            ThisRider = thisRider;
        }
    }
}
