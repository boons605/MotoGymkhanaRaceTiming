// <copyright file="RaceTracker.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Models;
using RiderIdUnit;
using TimingUnit;

namespace RaceManagement
{
    /// <summary>
    /// Class that keeps track of a race through events provided raised by timing and id units
    /// </summary>
    public class RaceTracker
    {
        /// <summary>
        /// The timing unit that contains the timing gates at the start and stop box
        /// </summary>
        private ITimingUnit timing;

        /// <summary>
        /// The id unit at the start box
        /// </summary>
        private IRiderIdUnit startGate;

        /// <summary>
        /// The id unit at the stop box
        /// </summary>
        private IRiderIdUnit endGate;

        /// <summary>
        /// The sensor id for the start timing gate connected to <see cref="timing"/>
        /// </summary>
        private int timingStartId;

        /// <summary>
        /// The sensor id for the end timing gate connected to <see cref="timing"/>
        /// </summary>
        private int timingEndId;

        /// <summary>
        /// The events triggered by riders entering the start box. In FIFO order so the first rider to enter the box is the first to start
        /// </summary>
        private ConcurrentQueue<EnteredEvent> waitingRiders = new ConcurrentQueue<EnteredEvent>();

        /// <summary>
        /// The matched id and time events that indicate a driver has left the start box and is on track. in FIFO order so the first element in this queue represent the rider that entered the track first
        /// </summary>
        private ConcurrentQueue<(EnteredEvent id, TimingEvent timer)> onTrackRiders = new ConcurrentQueue<(EnteredEvent id, TimingEvent timer)>();

        /// <summary>
        /// The complete list of events in chronological order
        /// </summary>
        private ConcurrentQueue<RaceEvent> raceState = new ConcurrentQueue<RaceEvent>();

        /// <summary>
        /// List of timing events picked up by the end timing gate that have not been matched to an id from <see cref="endIds"/>. Oldest first
        /// </summary>
        private List<TimingEvent> endTimes = new List<TimingEvent>();

        /// <summary>
        /// List of id events picked up by the end id unit that have not been matched to a time from <see cref="endTimes"/>. Oldest first
        /// </summary>
        private List<LeftEvent> endIds = new List<LeftEvent>();

        /// <summary>
        /// Since we are using non-concurrent data structures for the end times and ids we need this lock to synchronize access by the end id and end timing event handlers
        /// </summary>
        private object endLock = new object();

        /// <summary>
        /// Even though we are using concurrent data structures for the start events, there may be a race condition regarding to the OnRiderWaiting and OnStartEmpty events, so we need to synchronize
        /// </summary>
        private object waitingLock = new object();

        /// <summary>
        /// Fired when the system is ready for the next rider to trigger the start timing gate
        /// </summary>
        public event EventHandler<WaitingRiderEventArgs> OnRiderWaiting;

        /// <summary>
        /// Fired when a rider's lap time is known
        /// </summary>
        public event EventHandler<FinishedRiderEventArgs> OnRiderFinished;

        /// <summary>
        /// Fired when the system has no riders waiting to start
        /// </summary>
        public event EventHandler OnStartEmpty;

        public RaceTracker(ITimingUnit timing, IRiderIdUnit startGate, IRiderIdUnit endGate, int timingStartId, int timingEndId)
        {
            this.timing = timing;
            this.startGate = startGate;
            this.endGate = endGate;
            this.timingStartId = timingStartId;
            this.timingEndId = timingEndId;
        }

        /// <summary>
        /// Gives you an overview of the current state of the race
        /// Do not modify the returned objects
        /// </summary>
        public (List<EnteredEvent> waiting, List<(EnteredEvent id, TimingEvent timer)> onTrack, List<LeftEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) GetState =>
            (waitingRiders.ToList(), onTrackRiders.ToList(), endIds.ToList(), endTimes.ToList());

        /// <summary>
        /// Run a task that communicates with the timing and rider units to track the state of a race
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<RaceSummary> Run(CancellationToken token)
        {
            RegisterEvents();

            //run forever unless cancellation is requested
            //this class is just receiving events, no code required to actually drive any actions here
            await WaitForToken(token);

            return new RaceSummary(raceState.ToList());
        }

        /// <summary>
        /// Turns a cancellation token into an awaitable task, this lets us avoid a loop to check the token and sleep
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private Task WaitForToken(CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            token.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        private void RegisterEvents()
        {
            timing.OnTrigger += (_, args) => OnTimer(args);
            startGate.OnRiderId += (_, args) => OnStartId(args);
            endGate.OnRiderId += (_, args) => OnEndId(args);
        }

        /// <summary>
        /// When a rider enters the start box they are recorded as waiting to start.
        /// The next time the start timing is triggered, the oldest waiting rider will be recorded as on track
        /// </summary>
        /// <param name="args"></param>
        private void OnStartId(RiderIdEventArgs args)
        {
            lock (waitingLock)
            {
                EnteredEvent newEvent = new EnteredEvent(args.Received, args.Rider);
                waitingRiders.Enqueue(newEvent);
                raceState.Enqueue(newEvent);

                if (waitingRiders.Count == 1)
                {
                    OnRiderWaiting?.Invoke(this, new WaitingRiderEventArgs(newEvent));
                }
            }
        }

        /// <summary>
        /// When a rider enters the stop box they will be recored as having finished a lap. It is possible the id unit's range is a bit too big so if there is no mtahcing timing event we will store the id event.
        /// This method may register a rider as finished or dnf
        /// </summary>
        /// <param name="args"></param>
        private void OnEndId(RiderIdEventArgs args)
        {
            lock (endLock)
            {
                LeftEvent newEvent = new LeftEvent(args.Received, args.Rider);

                //if we receive an end id for a rider that is not on track ignore it
                if (!onTrackRiders.Any(t => t.id.Rider == newEvent.Rider))
                {
                    return;
                }

                raceState.Enqueue(newEvent);

                TimingEvent closest = endTimes.FirstOrDefault();

                //If the range on the id unit is larger than the stop box, we may receive an id event before a timing event
                if (closest == null)
                {
                    endIds.Add(newEvent);
                    return;
                }

                foreach (TimingEvent e in endTimes)
                {
                    if ((e.Time - args.Received).Duration() < (closest.Time - args.Received).Duration())
                    {
                        closest = e;
                    }
                }

                if ((closest.Time - args.Received).Duration().TotalSeconds <= 10)
                {
                    closest.SetRider(newEvent.Rider);
                    endTimes.Remove(closest);

                    MatchLapEnd(newEvent, closest);
                }
                else
                {
                    endIds.Add(newEvent);
                }
            }
        }

        /// <summary>
        /// When a gate of the timing unit is triggered. If its the start gate we will attempt to match it to a start id, if its the end gate we will attempt to match it to an end id.
        /// This method may register a rider as finished, on track or dnf
        /// </summary>
        /// <param name="args"></param>
        private void OnTimer(TimingTriggeredEventArgs args)
        {
            //When a waiting rider triggers the start timing unit, they are recorded as on track
            if (args.GateId == timingStartId)
            {
                lock (waitingLock)
                {
                    bool hasWaitingRider = waitingRiders.TryDequeue(out EnteredEvent rider);

                    if (hasWaitingRider)
                    {
                        TimingEvent newEvent = new TimingEvent(args.Received, rider.Rider, args.Microseconds, args.GateId);

                        onTrackRiders.Enqueue((rider, newEvent));
                        raceState.Enqueue(newEvent);

                        waitingRiders.TryPeek(out EnteredEvent waiting);
                        if (waiting != null)
                        {
                            OnRiderWaiting?.Invoke(this, new WaitingRiderEventArgs(waiting));
                        }
                        else
                        {
                            OnStartEmpty?.Invoke(this, EventArgs.Empty);
                        }
                    } //if we dont have a waiting rider, disregard event somebody probably walked through the beam
                }
            }
            else if (args.GateId == timingEndId)
            {
                //when a rider triggers the end timing unit, that must be matched to an end id unit event
                //if there is such a match, then it must be matched to an on track rider
                lock (endLock)
                {
                    //we dont know the rider yet
                    TimingEvent newEvent = new TimingEvent(args.Received, null, args.Microseconds, args.GateId);
                    raceState.Enqueue(newEvent);

                    LeftEvent closest = endIds.FirstOrDefault();

                    //if the rider id unit's range is smaller than the stop box, we may receive the rider id later
                    if (closest == null)
                    {
                        endTimes.Add(newEvent);
                        return;
                    }

                    foreach (LeftEvent e in endIds)
                    {
                        if ((e.Time - args.Received).Duration() < (closest.Time - args.Received).Duration())
                        {
                            closest = e;
                        }
                    }

                    if ((closest.Time - args.Received).Duration().TotalSeconds <= 10)
                    {
                        newEvent.SetRider(closest.Rider);
                        endIds.Remove(closest);

                        MatchLapEnd(closest, newEvent);
                    }
                    else
                    {
                        endTimes.Add(newEvent);
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Found unkown gate id in timing event: {args.GateId}. Known gates are start: {timingStartId}, end: {timingEndId}");
            }
        }

        /// <summary>
        /// This method tries to match a lap end, with an on track rider
        /// if the lap end matches the oldest on track rider, this rider gets a Finished event
        /// if the lap end matches any younger rider, that rider gets a Finished event and any older on track riders get a DNF event
        /// </summary>
        /// <param name="id"></param>
        /// <param name="time"></param>
        private void MatchLapEnd(LeftEvent endId, TimingEvent endTime)
        {
            List<(EnteredEvent startId, TimingEvent startTime)> dnf = new List<(EnteredEvent startId, TimingEvent startTime)>();

            FinishedEvent finish = null;
            while (onTrackRiders.TryDequeue(out (EnteredEvent startId, TimingEvent startTime) onTrack))
            {
                if (onTrack.startId.Rider == endId.Rider)
                {
                    finish = new FinishedEvent(onTrack.startId, onTrack.startTime, endTime, endId);
                    raceState.Enqueue(finish);
                    OnRiderFinished?.Invoke(this, new FinishedRiderEventArgs(finish));
                    break;
                }
                else
                {
                    //if there are older riders that do not match, they must have left the track without passing the stop box
                    //since they are older they appear earlier in the loop
                    dnf.Add(onTrack);
                }
            }

            foreach ((EnteredEvent startId, TimingEvent startTime) in dnf)
            {
                raceState.Enqueue(new DNFEvent(finish, startId));
            }

            //filter out all older events that can never be matched
            //if an event is more than 10 seconds older than its most recently finished counterpart it will never be matched
            endIds = endIds.Where(e => (e.Time - finish.TimeEnd.Time).TotalSeconds > -10).ToList();
            endTimes = endTimes.Where(e => (e.Time - finish.Left.Time).TotalSeconds > -10).ToList();
        }
    }

    public class FinishedRiderEventArgs
    {
        public FinishedEvent Finish { get; private set; }

        public FinishedRiderEventArgs(FinishedEvent finish)
        {
            Finish = finish;
        }
    }

    public class WaitingRiderEventArgs
    {
        public EnteredEvent Rider { get; private set; }

        public WaitingRiderEventArgs(EnteredEvent rider)
        {
            Rider = rider;
        }
    }
}
