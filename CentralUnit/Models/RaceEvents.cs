// <copyright file="RaceEvents.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    /// <summary>
    /// Base class for representing things that happened during a race. Every event has three basic properties: event id, who the event applies to and when the event was recorded
    /// </summary>
    [JsonConverter(typeof(EventJsonConverter))]
    public class RaceEvent
    {
        /// <summary>
        /// The moment the event was processed
        /// </summary>
        [JsonProperty]
        public DateTime Time;

        /// <summary>
        /// Unique id to refer to this event
        /// </summary>
        [JsonProperty]
        public readonly Guid EventId;

        /// <summary>
        /// The rider this event happened to
        /// </summary>
        [JsonProperty]
        public Rider Rider { get; protected set; }

        public RaceEvent(DateTime time, Rider rider, Guid eventId)
        {
            Time = time;
            Rider = rider;
            EventId = eventId;
        }

        // Below here is all the infrastructure to make the json deserialization figure out which kind of event it is dealing with

        /// <summary>
        /// When serialized this field contains a string like 'RiderReadyEvent' so consumers know which kind of event it is
        /// The consumer in this code os <see cref="EventJsonConverter"/>
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))] // Serialize enums by name rather than numerical value
        public RaceEventType Type { get { return TypeToEvent[GetType()]; } }

        static readonly Dictionary<Type, RaceEventType> TypeToEvent;
        static readonly Dictionary<RaceEventType, Type> EventToType;

        static RaceEvent()
        {
            TypeToEvent = new Dictionary<Type, RaceEventType>()
            {
                { typeof(RaceEvent), RaceEventType.RaceEvent},
                { typeof(ManualEvent), RaceEventType.ManualEvent },
                { typeof(RiderReadyEvent), RaceEventType.RiderReadyEvent },
                { typeof(RiderFinishedEvent), RaceEventType.RiderFinishedEvent },
                { typeof(FinishedEvent), RaceEventType.FinishedEvent },
                { typeof(TimingEvent), RaceEventType.TimingEvent },
                { typeof(ManualDNFEvent), RaceEventType.ManualDNFEvent },
                { typeof(DSQEvent), RaceEventType.DSQEvent },
                { typeof(PenaltyEvent), RaceEventType.PenaltyEvent },
                { typeof(ClearReadyEvent), RaceEventType.ClearReadyEvent },
                { typeof(DeleteTimeEvent), RaceEventType.DeleteTimeEvent },

            };
            EventToType = TypeToEvent.ToDictionary(pair => pair.Value, pair => pair.Key);
        }

        public static Type GetType(RaceEventType subType)
        {
            return EventToType[subType];
        }
    }

    /// <summary>
    /// Base class for events that are triggered by a user and not by the system itself
    /// </summary>
    public class ManualEvent : RaceEvent
    {
        /// <summary>
        /// Name of the official that issued the event
        /// </summary>
        [JsonProperty]
        public readonly string StaffName;

        public ManualEvent(DateTime time, Rider rider, Guid eventId, string staffName) : base(time, rider, eventId)
        {
            StaffName = staffName;
        }

        protected ManualEvent()
            :base(DateTime.MinValue, null, Guid.Empty)
        {
        }
    }

    /// <summary>
    /// Event to mark when a user signals that a rider is waiting to start
    /// </summary>
    public class RiderReadyEvent : ManualEvent
    {
        public RiderReadyEvent(DateTime time, Rider rider, Guid eventId, string staffName) : base(time, rider, eventId, staffName)
        {
        }

        protected RiderReadyEvent()
        {
        }
    }

    /// <summary>
    /// Event to mark when a user signals that a rider has finished
    /// </summary>
    public class RiderFinishedEvent : ManualEvent
    {
        [JsonProperty]
        public readonly TimingEvent TimeEnd;

        public RiderFinishedEvent(DateTime time, Rider rider, Guid eventId, string staffName, TimingEvent end) : base(time, rider, eventId, staffName)
        {
            TimeEnd = end;
        }

        protected RiderFinishedEvent()
        { 
        }
    }

    /// <summary>
    /// Event to mark when a rider has finished. A rider is finished when we have recorded 4 essential events: rider at start box, timing at start box, rider at end box and timing at end box
    /// </summary>
    public class FinishedEvent : RaceEvent
    {
        /// <summary>
        /// Lap time in microseconds
        /// </summary>
        public long LapTime => TimeEnd.Microseconds - TimeStart.Microseconds;

        /// <summary>
        /// The event where the user signalled the rider was waiting in the start box
        /// </summary>
        [JsonProperty]
        public readonly RiderReadyEvent Entered;

        /// <summary>
        /// The event where the start timing gate is triggered by the rider
        /// </summary>
        [JsonProperty]
        public readonly TimingEvent TimeStart;

        /// <summary>
        /// The event where the end timing gate is triggered by the rider
        /// </summary>
        public TimingEvent TimeEnd => Left.TimeEnd;

        /// <summary>
        /// The event where the user signalled the rider had finished
        [JsonProperty]
        public readonly RiderFinishedEvent Left;

        public FinishedEvent(RiderReadyEvent entered, TimingEvent timeStart, RiderFinishedEvent left, Guid eventId = new Guid())
            : base(left.TimeEnd.Time, left.Rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId)
        {
            Entered = entered;
            TimeStart = timeStart;
            Left = left;
        }

        protected FinishedEvent()
            :base(DateTime.MinValue, null, Guid.Empty)
        {
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
        [JsonProperty]
        public readonly long Microseconds;

        /// <summary>
        /// Which timing gate this event happened for
        /// </summary>
        [JsonProperty]
        public readonly int GateId;

        public TimingEvent(DateTime time, Rider rider, long microseconds, int gateId, Guid eventId = new Guid()) 
            : base(time, rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId)
        {
            Microseconds = microseconds;
            GateId = gateId;
        }

        protected TimingEvent()
            :base(DateTime.MinValue, null, Guid.Empty)
        {
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

    public class ManualDNFEvent : ManualEvent
    {
        /// <summary>
        /// The event where this driver was picked up at the start gate
        /// </summary>
        [JsonProperty]
        public readonly RiderReadyEvent ThisRider;

        public ManualDNFEvent(RiderReadyEvent started, string staffName, Guid eventId = new Guid()) 
            : base(started.Time, started.Rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId, staffName)
        {
            ThisRider = started;
        }

        protected ManualDNFEvent()
        {
        }
    }

    /// <summary>
    /// Event when a race official disqualifies a lap
    /// Can be issued while the rider is on track as well as after a rider is finished
    /// </summary>
    public class DSQEvent : ManualEvent
    {
        /// <summary>
        /// Why this event was issued
        /// </summary>
        [JsonProperty]
        public readonly string Reason;

        public DSQEvent(DateTime time, Rider rider, string staffName, string reason, Guid eventId = new Guid()) 
            : base(time, rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId, staffName)
        {
            Reason = reason;
        }

        protected DSQEvent()
        {
        }
    }

    public class PenaltyEvent : ManualEvent
    {
        /// <summary>
        /// Why this penalty was issued
        /// </summary>
        [JsonProperty]
        public readonly string Reason;

        /// <summary>
        /// How many seconds are to be added to the lap time
        /// </summary>
        [JsonProperty]
        public readonly int Seconds;

        public PenaltyEvent(DateTime time, Rider rider, string reason, int seconds, string staffName, Guid eventId = new Guid()) 
            : base(time, rider, eventId == Guid.Empty ? Guid.NewGuid() : eventId, staffName)
        {
            Reason = reason;
            Seconds = seconds;
        }

        protected PenaltyEvent()
        {
        }
    }

    public class ClearReadyEvent : ManualEvent
    {
        public ClearReadyEvent(DateTime time, Rider rider, Guid eventId, string staffName) : base(time, rider, eventId, staffName)
        {
        }

        protected ClearReadyEvent()
        {
        }
    }

    public class DeleteTimeEvent : ManualEvent
    {
        /// <summary>
        /// Which time event should be deleted
        /// </summary>
        [JsonProperty]
        public readonly Guid TargetEventId;

        public DeleteTimeEvent(DateTime time, Guid targetId, string staffName, Guid eventId = new Guid()) 
            : base(time, null, eventId == Guid.Empty ? Guid.NewGuid() : eventId, staffName)
        {
            TargetEventId = targetId;
        }

        protected DeleteTimeEvent()
        {
        }
    }

    public class EventJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RaceEvent);
        }

        public override bool CanWrite { get { return false; } }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            JToken typeToken = token["Type"];
            if (typeToken == null)
                throw new InvalidOperationException("invalid object");
            Type actualType = RaceEvent.GetType(typeToken.ToObject<RaceEventType>(serializer));
            if (existingValue == null || existingValue.GetType() != actualType)
            {
                Newtonsoft.Json.Serialization.JsonContract contract = serializer.ContractResolver.ResolveContract(actualType);
                existingValue = contract.DefaultCreator();
            }
            using (JsonReader subReader = token.CreateReader())
            {
                // Using "populate" avoids infinite recursion.
                serializer.Populate(subReader, existingValue);
            }
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public enum RaceEventType
    {
        RaceEvent,
        ManualEvent,
        RiderReadyEvent,
        RiderFinishedEvent,
        FinishedEvent,
        TimingEvent,
        ManualDNFEvent,
        DSQEvent,
        PenaltyEvent,
        ClearReadyEvent,
        DeleteTimeEvent
    }
}
