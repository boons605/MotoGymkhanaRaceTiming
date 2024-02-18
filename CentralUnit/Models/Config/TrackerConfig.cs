// <copyright file="TrackerConfig.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Config
{
    /// <summary>
    /// This config contains the ids for that start and end timing gates, so the tracker knows which events start a lap and which end a lap
    /// </summary>
    public class TrackerConfig
    {
        /// <summary>
        /// Start gate id
        /// </summary>
        public int StartTimingGateId { get; set; }

        /// <summary>
        /// End gate id
        /// </summary>
        public int EndTimingGateId { get; set; }
    }
}
