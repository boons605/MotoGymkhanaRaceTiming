// <copyright file="RaceSummary.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Models
{
    /// <summary>
    /// This class lists all the events that happened during a race. In the future should also be able to save/load summaries and provide race statistics
    /// </summary>
    public class RaceSummary
    {
        /// <summary>
        /// The events that happened in the race in the order that were processed
        /// </summary>
        public List<RaceEvent> Events { get; private set; }

        /// <summary>
        /// All the riders that participated in this race
        /// </summary>
        public List<Rider> Riders => Events
                .Select(e => e.Rider)
                .Distinct(new RiderNameEquality())
                .ToList();

        /// <summary>
        /// For json (de)serialization.
        /// </summary>
        public RaceSummary()
        {
        }

        /// <summary>
        /// Constructor for general use. Riders will be collected from events
        /// </summary>
        /// <param name="events"></param>
        public RaceSummary(List<RaceEvent> events)
        {
            Events = events;
        }

        /// <summary>
        /// Writes a JSON to the stream that represents the race
        /// </summary>
        /// <param name="output">the stream to write to</param>
        public void WriteSummary(Stream output)
        {
            JArray riders = JArray.FromObject(this.Riders);

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new RiderConverter(null));
            serializer.TypeNameHandling = TypeNameHandling.Auto;

            JArray events = JArray.FromObject(this.Events, serializer);

            JObject composite = new JObject();
            composite.Add("Riders", riders);
            composite.Add("Events", events);

            using (StreamWriter writer = new StreamWriter(output, System.Text.Encoding.UTF8, 1024, true))//we dont own the stream, so dont close it when the writer closes
            {
                writer.WriteLine(JsonConvert.SerializeObject(composite));
            }
        }

        public static RaceSummary ReadSummary(Stream input)
        {
            using (StreamReader reader = new StreamReader(input, System.Text.Encoding.UTF8, false, 1024, true))
            {
                JObject intermediate = JObject.Parse(reader.ReadToEnd());

                List<Rider> riders = intermediate["Riders"].ToObject<List<Rider>>();

                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new RiderConverter(riders));
                serializer.TypeNameHandling = TypeNameHandling.Auto;

                List<RaceEvent> events = intermediate["Events"].ToObject<List<RaceEvent>>(serializer);

                return new RaceSummary(events);
            }
        }

        /// <summary>
        /// Often when serializing events you do not want to repeat all the information contained in the Rider for every events
        /// This serializer simplifies it to just the name. Intended to be used when a separate list of riders is available to match names with on deserialization
        /// </summary>
        private class RiderConverter : JsonConverter<Rider>
        {
            private readonly List<Rider> riders;
            public RiderConverter(List<Rider> riders)
            {
                this.riders = riders;
            }

            public override Rider ReadJson(JsonReader reader, Type objectType, Rider existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                string name = (string)reader.Value;

                Rider replacement = riders.Find(r => r.Name == name);

                return replacement;
            }

            public override void WriteJson(JsonWriter writer, Rider value, JsonSerializer serializer)
            {
                writer.WriteValue(value.Name);
            }
        }

        private class RiderNameEquality : IEqualityComparer<Rider>
        {
            public bool Equals(Rider x, Rider y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(Rider obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
