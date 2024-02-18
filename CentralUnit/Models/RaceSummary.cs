// <copyright file="RaceSummary.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Models.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        /// The config used to run this race
        /// </summary>
        public TrackerConfig Config { get; private set; }

        /// <summary>
        /// All the riders that participated in this race
        /// </summary>
        public List<Rider> Riders => Events
                .Select(e => e.Rider)
                .Where(e => e != null)
                .Distinct(new RiderNameEquality())
                .ToList();

        /// <summary>
        /// Initializes a new instance of the <see cref="RaceSummary" /> class.
        /// For JSON (de)serialization.
        /// </summary>
        public RaceSummary()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RaceSummary" /> class.
        /// Constructor for general use. Riders will be collected from events
        /// </summary>
        /// <param name="events">everything that happened during the race</param>
        /// <param name="config">the config used to run the race</param>
        public RaceSummary(List<RaceEvent> events, TrackerConfig config)
        {
            Events = events;
            Config = config;
        }

        /// <summary>
        /// Parses a summary in JSON format from a stream
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static RaceSummary ReadSummary(Stream input)
        {
            using (StreamReader reader = new StreamReader(input, System.Text.Encoding.UTF8, false, 1024, true))
            {
                JObject intermediate = JObject.Parse(reader.ReadToEnd());

                List<Rider> riders = intermediate["Riders"].ToObject<List<Rider>>();

                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new RiderConverter(riders));

                List<RaceEvent> events = intermediate["Events"].ToObject<List<RaceEvent>>(serializer);

                return new RaceSummary(events, intermediate["Config"].ToObject<TrackerConfig>());
            }
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

            JArray events = JArray.FromObject(this.Events, serializer);

            JObject composite = new JObject();
            composite.Add("Riders", riders);
            composite.Add("Events", events);
            composite.Add("Config", JObject.FromObject(Config));

            // we dont own the stream, so dont close it when the writer closes
            using (StreamWriter writer = new StreamWriter(output, System.Text.Encoding.UTF8, 1024, true)) 
            {
                writer.WriteLine(JsonConvert.SerializeObject(composite));
            }
        }

        /// <summary>
        /// Often when serializing events you do not want to repeat all the information contained in the Rider for every events
        /// This serializer simplifies it to just the name. Intended to be used when a separate list of riders is available to match names with on deserialization
        /// </summary>
        private class RiderConverter : JsonConverter<Rider>
        {
            /// <summary>
            /// Riders to be encoded and decoded
            /// </summary>
            private readonly List<Rider> riders;

            /// <summary>
            /// Initializes a new instance of the <see cref="RiderConverter" /> class.
            /// </summary>
            /// <param name="riders"></param>
            public RiderConverter(List<Rider> riders)
            {
                this.riders = riders;
            }

            /// <summary>
            /// Overrides that standard JSON read function to read the rider name and look up the full rider
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="objectType"></param>
            /// <param name="existingValue"></param>
            /// <param name="hasExistingValue"></param>
            /// <param name="serializer"></param>
            /// <returns>The full rider with the parsed name</returns>
            public override Rider ReadJson(JsonReader reader, Type objectType, Rider existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                string name = (string)reader.Value;

                Rider replacement = riders.Find(r => r.Name == name);

                return replacement;
            }

            /// <summary>
            /// Overrides that standard writing function to only write the rider name instead of the full object
            /// </summary>
            /// <param name="writer"></param>
            /// <param name="value"></param>
            /// <param name="serializer"></param>
            public override void WriteJson(JsonWriter writer, Rider value, JsonSerializer serializer)
            {
                writer.WriteValue(value.Name);
            }
        }

        /// <summary>
        /// Compares rider names
        /// </summary>
        private class RiderNameEquality : IEqualityComparer<Rider>
        {
            /// <summary>
            /// Runs standard string equality for rider names
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public bool Equals(Rider x, Rider y)
            {
                return x.Name == y.Name;
            }

            /// <summary>
            /// Returns GetHashCode of rider name
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public int GetHashCode(Rider obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
