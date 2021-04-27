using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Models
{
    public class RaceConfig
    {
        public string TimingUnitId { get; set; }
        public string StartIdUnitId { get; set; }
        public string EndIdUnitId { get; set; }
        public int StartTimingGateId { get; set; }
        public int EndTimingGateId { get; set; }
        public double StartIdRange { get; set; }
        public double EndIdRange { get; set; }

    }
}
