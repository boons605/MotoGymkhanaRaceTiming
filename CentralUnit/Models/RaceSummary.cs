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
        public List<Rider> Riders { get; private set; }

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

            Riders = events
                .Select(e => e.Rider)
                .Distinct(new RiderNameEquality())
                .ToList();
        }

        /// <summary>
        /// Writes a JSON to the stream that represents the race
        /// </summary>
        /// <param name="output">the stream to write to</param>
        public void WriteSummary(Stream output)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new EventRiderConverter(null));
            settings.ContractResolver = new NonPublicPropertiesResolver();
            settings.TypeNameHandling = TypeNameHandling.Auto;

            using (StreamWriter writer = new StreamWriter(output, System.Text.Encoding.UTF8, 1024, true))//we dont own the stream, so dont close it when the writer closes
            {
                writer.WriteLine(JsonConvert.SerializeObject(this, settings));
            }
        }

        public static RaceSummary ReadSummary(Stream input)
        {
            using (StreamReader reader = new StreamReader(input, System.Text.Encoding.UTF8, false, 1024, true))
            {
                JObject intermediate = JObject.Parse(reader.ReadToEnd());

                JsonSerializer serializer = new JsonSerializer();
                serializer.ContractResolver = new NonPublicPropertiesResolver();
                serializer.TypeNameHandling = TypeNameHandling.Auto;

                List<Rider> riders = intermediate["Riders"].ToObject<List<Rider>>(serializer);

                serializer.Converters.Add(new EventRiderConverter(riders));

                return intermediate.ToObject<RaceSummary>(serializer);
            }
        }

        /// <summary>
        /// Often when serializing events you do not want to repeat all the information contained in the Rider for every events
        /// This serializer simplifies it to just the name. If you want to preserve the beacon information you should either not use this serializer or serialize the full rider objects separately
        /// </summary>
        private class EventRiderConverter : JsonConverter<RaceEvent>
        {
            private List<Rider> riders;
            public EventRiderConverter(List<Rider> riders)
            {
                this.riders = riders;
            }

            public override RaceEvent ReadJson(JsonReader reader, Type objectType, [AllowNullAttribute] RaceEvent existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JObject intermediate = JObject.Parse((string)reader.Value);

                string name = intermediate["Rider"].ToString();

                Rider replacement = riders.Find(r => r.Name == name);

                if (replacement == null)
                    throw new JsonReaderException($"Could not find rider {(string)reader.Value} in provided riders");

                intermediate["Rider"] = JObject.FromObject(replacement);

                JsonSerializer privateSerializer = new JsonSerializer();
                serializer.ContractResolver = new NonPublicPropertiesResolver();
                serializer.TypeNameHandling = TypeNameHandling.Auto;

                return intermediate.ToObject<RaceEvent>(privateSerializer);
            }

            public override void WriteJson(JsonWriter writer, [AllowNullAttribute] RaceEvent value, JsonSerializer serializer)
            {
                JObject intermediate = JObject.FromObject(value);

                intermediate["Rider"] = value.Rider.Name;

                writer.WriteValue(intermediate.ToString());
            }
        }

        private class NonPublicPropertiesResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                if (member is PropertyInfo pi)
                {
                    prop.Readable = (pi.GetMethod != null);
                    prop.Writable = (pi.SetMethod != null);
                }
                return prop;
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
