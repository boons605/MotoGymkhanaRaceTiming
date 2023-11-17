using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Models.Config
{
    public class RaceConfig
    {
        [JsonRequired]
        public string TimingUnitId { get; set; }

        [JsonRequired]
        public string StartLightUnitId { get; set; }

        [JsonRequired]
        public int StartTimingGateId { get; set; }

        [JsonRequired]
        public int EndTimingGateId { get; set; }

        public TrackerConfig ExtractTrackerConfig() => new TrackerConfig
        {
            StartTimingGateId = this.StartTimingGateId,
            EndTimingGateId = this.EndTimingGateId,
        };
    }
}
