// <copyright file="RaceConfig.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Models.Config
{
    /// <summary>
    /// This config contains all the ids for the timing hardware modules necessary to run a race
    /// </summary>
    public class RaceConfig
    {
        /// <summary>
        /// Id for the central hardware unit that controls all the others, also includes a display for times
        /// </summary>
        [JsonRequired]
        public string TimingUnitId { get; set; }

        /// <summary>
        /// Id for the hardware unit that tells riders if they can start
        /// </summary>
        [JsonRequired]
        public string StartLightUnitId { get; set; }

        /// <summary>
        /// Id for the hardware unit that produces a timestamp when a rider starts
        /// </summary>
        [JsonRequired]
        public int StartTimingGateId { get; set; }

        /// <summary>
        /// Id for the hardware unit that produces a timestamp when a rider finishes
        /// </summary>
        [JsonRequired]
        public int EndTimingGateId { get; set; }

        /// <summary>
        /// Gets the timing gate ids from the full config
        /// </summary>
        /// <returns></returns>
        public TrackerConfig ExtractTrackerConfig() => new TrackerConfig
        {
            StartTimingGateId = this.StartTimingGateId,
            EndTimingGateId = this.EndTimingGateId,
        };
    }
}
