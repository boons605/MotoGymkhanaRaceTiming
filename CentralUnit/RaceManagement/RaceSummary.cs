using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RaceManagement
{
    public class RaceSummary
    {
        List<RaceEvent> Events = new List<RaceEvent>();

        public void WriteSummary(Stream output)
        {
            throw new NotImplementedException();
        }
    }

    public class RaceEvent
    {
        public readonly DateTime Time;
        public string Rider {get; protected set; }
        public readonly Guid EventId;
    }

    public class FinishedEvent : RaceEvent
    {
        /// <summary>
        /// Lap time in microseconds
        /// </summary>
        long LapTime => TimeEnd.Microseconds - TimeStart.Microseconds;

        /// <summary>
        /// A racer finishes when their id is picked up at the start gate, with a timing event and then their id is picked up at the end gate with a timing event
        /// </summary>
        EnteredEvent Entered;
        TimingEvent TimeStart;
        TimingEvent TimeEnd;
        LeftEvent Left;
    }

    /// <summary>
    /// Event when the rider id is picked up at the start gate
    /// </summary>
    public class EnteredEvent : RaceEvent
    {
        /// <summary>
        /// Id reported to the sensor that registered the rider
        /// </summary>
        byte[] SensorId;
    }

    /// <summary>
    /// Event when the rider id is picked up at the end gate
    /// </summary>
    public class LeftEvent : RaceEvent
    {
        /// <summary>
        /// Id reported to the sensor that registered the rider
        /// </summary>
        byte[] SensorId;
    }

    public class TimingEvent
    {
        /// <summary>
        /// Microseconds reported by the timer
        /// </summary>
        public readonly long Microseconds;

        //Which timing gate this event happened for
        int gateId;
    }

    public class DNFEvent : RaceEvent
    {
        /// <summary>
        /// A DNF happens when a driver who started later finished before this driver did
        /// </summary>
        FinishedEvent OtherDriver;

        /// <summary>
        /// The event where this driver was picked up at the start gate
        /// </summary>
        EnteredEvent ThisDriver;
    }
}
