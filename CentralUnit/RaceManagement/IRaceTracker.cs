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
        (List<EnteredEvent> waiting, List<(EnteredEvent id, TimingEvent timer)> onTrack, List<LeftEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) GetState { get; }

        event EventHandler<DNFRiderEventArgs> OnRiderDNF;
        event EventHandler<FinishedRiderEventArgs> OnRiderFinished;
        event EventHandler<WaitingRiderEventArgs> OnRiderWaiting;
        event EventHandler OnStartEmpty;

        Task<RaceSummary> Run(CancellationToken token);
    }
}