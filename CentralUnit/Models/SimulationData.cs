using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    /// <summary>
    /// This class can serialize and deserialize a list of timing events
    /// Since the timing events are the only system generated events during a race the files produced and read by this class can be used to simulate a race
    /// </summary>
    public class SimulationData
    {
        public int StartGateId { get; set; }
        public int EndGateId { get; set; }

        public List<SimulatedTimingEvent> Events { get; set; }

        public List<Rider> Riders { get; set; }

        public SimulationData(int startGateId, int endGateId, List<SimulatedTimingEvent> simulatedTimingEvents, List<Rider> riders)
        {
            StartGateId = startGateId;
            EndGateId = endGateId;
            Events = simulatedTimingEvents;
            Riders = riders;
        }
    }

    public class SimulatedTimingEvent
    {
        public int GateId { get; set; }
        public int Microseconds { get; set; }
        public int MillisecondsFromSimStart { get; set; }
    }
}
