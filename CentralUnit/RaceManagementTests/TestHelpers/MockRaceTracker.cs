﻿using Models;
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
        public (List<IdEvent> waiting, List<(IdEvent id, TimingEvent timer)> onTrack, List<IdEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) GetState => throw new NotImplementedException();

        public List<Lap> Laps => new List<Lap>();

        public event EventHandler<FinishedRiderEventArgs> OnRiderDNF;
        public event EventHandler<FinishedRiderEventArgs> OnRiderFinished;
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

        public void RemoveRider(string name)
        {
            //no action
        }

        public void AddEvent<T>(T raceEvent) where T : ManualEventArgs
        {
            throw new NotImplementedException();
        }
    }
}
