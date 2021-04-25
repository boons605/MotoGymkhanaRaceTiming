// <copyright file="RaceTracker.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RaceManagement
{
    public interface IRaceTracker
    {
        (List<IdEvent> waiting, List<(IdEvent id, TimingEvent timer)> onTrack, List<IdEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) GetState { get; }

        event EventHandler<DNFRiderEventArgs> OnRiderDNF;
        event EventHandler<FinishedRiderEventArgs> OnRiderFinished;
        event EventHandler<WaitingRiderEventArgs> OnRiderWaiting;
        event EventHandler OnStartEmpty;

        Task<RaceSummary> Run(CancellationToken token);
        void AddRider(Rider rider);
        void RemoveRider(string name);
    }
}