using Models;
using RaceManagement;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RaceManagementTests.TestHelpers
{
    public class MockRaceTracker : IRaceTracker
    {
        public (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) GetState => throw new NotImplementedException();

        public List<Lap> Laps => new List<Lap>();

        public event EventHandler<LapCompletedEventArgs> OnRiderDNF;
        public event EventHandler<LapCompletedEventArgs> OnRiderMatched;
        public event EventHandler<WaitingRiderEventArgs> OnRiderWaiting;
        public event EventHandler OnStartEmpty;

        public Task<RaceSummary> Run(CancellationToken token)
        {
            return Task.FromResult(new RaceSummary());
        }

        public void AddRider(Rider rider)
        {
            //no action
        }

        public void RemoveRider(Guid id)
        {
            //no action
        }

        public void AddEvent<T>(T raceEvent) where T : EventArgs
        {
            throw new NotImplementedException();
        }

        public Rider GetRiderById(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
