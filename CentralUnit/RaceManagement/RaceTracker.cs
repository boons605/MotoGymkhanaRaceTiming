using RiderIdUnit;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimingUnit;

namespace RaceManagement
{
    public class RaceTracker
    {
        private ITimingUnit Timing;
        private IRiderIdUnit StartGate, EndGate;
        private int TimingStartId, TimingEndId;

        public RaceTracker(ITimingUnit timing, IRiderIdUnit startGate, IRiderIdUnit endGate, int timingStartId, int timingEndId)
        {
            Timing = timing;
            StartGate = startGate;
            EndGate = endGate;
            TimingStartId = timingStartId;
            TimingEndId = timingEndId;
        }

        /// <summary>
        /// Run a task that communicates with the timing and rider units to track the state of a race
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<RaceSummary> Run(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
