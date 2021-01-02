using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RaceManagement
{
    public class RaceSummary
    {
        public List<RaceEvent> Events { get; private set; }

        public RaceSummary(List<RaceEvent> events)
        {
            Events = events;
        }

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

        public RaceEvent(DateTime time, string rider, Guid eventId)
        {
            Time = time;
            Rider = rider;
            EventId = eventId;
        }

        public RaceEvent(DateTime time, string rider)
            :this(time, rider, Guid.NewGuid())
        {
            Time = time;
            Rider = rider;
        }
    }

    public class FinishedEvent : RaceEvent
    {
        /// <summary>
        /// Lap time in microseconds
        /// </summary>
        public long LapTime => TimeEnd.Microseconds - TimeStart.Microseconds;

        /// <summary>
        /// A racer finishes when their id is picked up at the start gate, with a timing event and then their id is picked up at the end gate with a timing event
        /// </summary>
        public readonly EnteredEvent Entered;
        public readonly TimingEvent TimeStart;
        public readonly TimingEvent TimeEnd;
        public readonly LeftEvent Left;

        public FinishedEvent(EnteredEvent entered, TimingEvent timeStart, TimingEvent timeEnd, LeftEvent left)
            :base(timeEnd.Time, timeEnd.Rider)
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
        /// <summary>
        /// Id reported to the sensor that registered the rider
        /// </summary>
        public byte[] SensorId { get; private set; }

        public EnteredEvent(DateTime time, string rider, byte[] sensorId)
            :base(time, rider)
        {
            SensorId = sensorId;
        }
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

        public LeftEvent(DateTime time, string rider, byte[] sensorId)
         : base(time, rider)
        {
            SensorId = sensorId;
        }
    }

    public class TimingEvent : RaceEvent
    {
        /// <summary>
        /// Microseconds reported by the timer
        /// </summary>
        public readonly long Microseconds;

        //Which timing gate this event happened for
        public readonly int GateId;

        public TimingEvent(DateTime time, string rider, long microseconds, int gateId) : base(time, rider)
        {
            Microseconds = microseconds;
            GateId = gateId;
        }

        /// <summary>
        /// For the end timing gate we may receive a timing event before a rider id
        /// So we might have to set this field after we've matched it
        /// </summary>
        /// <param name="rider"></param>
        public void SetRider(string rider)
        {
            Rider = rider;
        }
    }

    public class DNFEvent : RaceEvent
    {
        /// <summary>
        /// A DNF happens when a driver who started later finished before this driver did
        /// </summary>
        FinishedEvent OtherRider;

        /// <summary>
        /// The event where this driver was picked up at the start gate
        /// </summary>
        EnteredEvent RiderDriver;

        public DNFEvent(FinishedEvent otherRider, EnteredEvent thisRider)
            :base(otherRider.Time, thisRider.Rider)
        {
            OtherRider = otherRider;
            this.RiderDriver = thisRider;
        }
    }
}
