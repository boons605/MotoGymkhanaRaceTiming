namespace Models.Config
{
    public class RaceConfig
    {
        public string TimingUnitId { get; set; }
        public string StartIdUnitId { get; set; }
        public string EndIdUnitId { get; set; }
        public int StartTimingGateId { get; set; }
        public int EndTimingGateId { get; set; }
        /// <summary>
        /// The maximum distance in meters where a bluetooth singal is considered in range of the start id unit
        /// </summary>
        public double StartIdRange { get; set; }
        /// <summary>
        /// The maximum distance in meters where a bluetooth singal is considered in range of the end id unit
        /// </summary>
        public double EndIdRange { get; set; }
        /// <summary>
        /// The maximum time in seconds there may be between and end time event and an end id event to be matched
        /// </summary>
        public int EndMatchTimeout { get; set; }

        public TrackerConfig ExtractTrackerConfig() => new TrackerConfig
        {
            StartTimingGateId = this.StartTimingGateId,
            EndTimingGateId = this.EndTimingGateId,
            EndMatchTimeout = this.EndMatchTimeout
        };
    }
}
