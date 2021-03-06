﻿// <copyright file="RaceEvents.cs" company="Moto Gymkhana">
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
        public readonly DateTime Time;

        /// <summary>
        /// Unique id to refer to this event
        /// </summary>
        public readonly Guid EventId;

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

    public class ManualEvent : RaceEvent
    {
        /// <summary>
        /// Name of the official that issued the event
        /// </summary>
        public readonly string StaffName;

        public ManualEvent(DateTime time, Rider rider, Guid eventId, string staffName) : base(time, rider, eventId)
        {
            StaffName = staffName;
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
        public readonly IdEvent Entered;

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
        public readonly IdEvent Left;

        public FinishedEvent(IdEvent entered, TimingEvent timeStart, TimingEvent timeEnd, IdEvent left, Guid eventId = new Guid())
            : base(timeEnd.Time, timeEnd.Rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId)
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
    public class IdEvent : RaceEvent
    {
        public readonly string UnitId;
        public readonly Direction IdType;

        public IdEvent(DateTime time, Rider rider, string unitId, Direction idType, Guid eventId = new Guid())
            : base(time, rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId) 
        {
            UnitId = unitId;
            IdType = idType;
        }
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

        public TimingEvent(DateTime time, Rider rider, long microseconds, int gateId, Guid eventId = new Guid()) 
            : base(time, rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId)
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
    public class UnitDNFEvent : RaceEvent
    {
        /// <summary>
        /// A DNF happens when a driver who started later finished before this driver did
        /// </summary>
        public readonly FinishedEvent OtherRider;

        /// <summary>
        /// The event where this driver was picked up at the start gate
        /// </summary>
        public readonly IdEvent ThisRider;

        public UnitDNFEvent(FinishedEvent otherRider, IdEvent thisRider, Guid eventId = new Guid())
            : base(otherRider.Time, thisRider.Rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId)
        {
            OtherRider = otherRider;
            ThisRider = thisRider;
        }

    }

    public class ManualDNFEvent : ManualEvent
    {
        /// <summary>
        /// The event where this driver was picked up at the start gate
        /// </summary>
        public readonly IdEvent ThisRider;

        public ManualDNFEvent(IdEvent started, string staffName, Guid eventId = new Guid()) 
            : base(started.Time, started.Rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId, staffName)
        {
        }
    }

    /// <summary>
    /// Event when a race official disqualifies a lap
    /// Can be issued while the rider is on track as well as after a rider is finished
    /// </summary>
    public class DSQEvent : ManualEvent
    {
        public readonly string Reason;

        public DSQEvent(DateTime time, Rider rider, string staffName, string reason, Guid eventId = new Guid()) 
            : base(time, rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId, staffName)
        {
            Reason = reason;
        }
    }

    public class PenaltyEvent : ManualEvent
    {
        public readonly string Reason;
        public readonly int Seconds;

        public PenaltyEvent(DateTime time, Rider rider, string reason, int seconds, string staffName, Guid eventId = new Guid()) 
            : base(time, rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId, staffName)
        {
            Reason = reason;
            Seconds = seconds;
        }
    }

    public enum Direction
    {
        Enter, Exit
    }
}
