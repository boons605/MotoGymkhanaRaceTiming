// <copyright file="SimulationData.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Models
{
    /// <summary>
    /// This class can serialize and deserialize a list of timing events
    /// Since the timing events are the only system generated events during a race the files produced and read by this class can be used to simulate a race
    /// </summary>
    public class SimulationData
    {
        /// <summary>
        /// The gate id in the simulated events that should start a lap
        /// </summary>
        public int StartGateId { get; set; }

        /// <summary>
        /// The gate id in the simulated events that should end a lap
        /// </summary>
        public int EndGateId { get; set; }

        /// <summary>
        /// The timing events that happen during this simulation
        /// </summary>
        public List<SimulatedTimingEvent> Events { get; set; }

        /// <summary>
        /// The riders that can start in the simulation
        /// </summary>
        public List<Rider> Riders { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulationData" /> class.
        /// </summary>
        /// <param name="startGateId">id in the simulatedTimingEvents for events that start laps</param>
        /// <param name="endGateId">id in the simulatedTimingEvents for events that end laps</param>
        /// <param name="simulatedTimingEvents">the timing events that happen during the race</param>
        /// <param name="riders">riders that participate in the race</param>
        public SimulationData(int startGateId, int endGateId, List<SimulatedTimingEvent> simulatedTimingEvents, List<Rider> riders)
        {
            StartGateId = startGateId;
            EndGateId = endGateId;
            Events = simulatedTimingEvents;
            Riders = riders;
        }
    }
    
    /// <summary>
    /// A user defined timing event.
    /// The given gate id should match the config
    /// </summary>
    public class SimulatedTimingEvent
    {
        /// <summary>
        /// The timing gate id, determines if this starts or ends a lap
        /// </summary>
        public int GateId { get; set; }

        /// <summary>
        /// Microseconds for this timing event, combined with the microseconds in other events to make a lap time
        /// </summary>
        public int Microseconds { get; set; }

        /// <summary>
        /// When this event should be fired
        /// </summary>
        public int MillisecondsFromSimStart { get; set; }
    }
}
