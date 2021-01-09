using RiderIdUnit;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimingUnit;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

namespace RaceManagement
{
    public class RaceTracker
    {
        private ITimingUnit Timing;
        private IRiderIdUnit StartGate, EndGate;
        private int TimingStartId, TimingEndId;

        private ConcurrentQueue<EnteredEvent> WaitingRiders = new ConcurrentQueue<EnteredEvent>();
        private ConcurrentQueue<(EnteredEvent id, TimingEvent timer)> OnTrackRiders = new ConcurrentQueue<(EnteredEvent id, TimingEvent timer)>();
        private ConcurrentQueue<RaceEvent> RaceState = new ConcurrentQueue<RaceEvent>();

        private List<TimingEvent> EndTimes = new List<TimingEvent>();
        private List<LeftEvent> EndIds = new List<LeftEvent>();

        //since we are using non-concurrent datastructures for the end events, we need this lock
        private object EndLock = new object();

        //to prevent a race conditionn with the start id unit and the start timing gate triggereing events, we need this lock
        private object WaitingLock = new object();

        public event EventHandler<WaitingRiderEventArgs> OnRiderWaiting;
        public event EventHandler<FinishedRiderEventArgs> OnRiderFinished;
        public event EventHandler OnStartEmpty;

        public RaceTracker(ITimingUnit timing, IRiderIdUnit startGate, IRiderIdUnit endGate, int timingStartId, int timingEndId)
        {
            Timing = timing;
            StartGate = startGate;
            EndGate = endGate;
            TimingStartId = timingStartId;
            TimingEndId = timingEndId;
        }

        /// <summary>
        /// Gives you an overview of the current state of the race
        /// Do not modify the returned objects
        /// </summary>
        public (List<EnteredEvent> waiting, List<(EnteredEvent id, TimingEvent timer)> onTrack, List<LeftEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) GetState =>
            (WaitingRiders.ToList(), OnTrackRiders.ToList(), EndIds.ToList(), EndTimes.ToList());

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

            return new RaceSummary(RaceState.ToList());
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
            Timing.OnTrigger += (_, args) => OnTimer(args);
            StartGate.OnRiderId += (_, args) => OnStartId(args);
            EndGate.OnRiderId += (_, args) => OnEndId(args);
        }

        /// <summary>
        /// When a rider enters the start box they are recorded as waiting to start.
        /// The next time the start timing is triggered, the oldest waiting rider will be recorded as on track
        /// </summary>
        /// <param name="args"></param>
        private void OnStartId(RiderIdEventArgs args)
        {
            lock (WaitingLock)
            {
                EnteredEvent newEvent = new EnteredEvent(args.Received, args.RiderName, args.SensorId);
                WaitingRiders.Enqueue(newEvent);
                RaceState.Enqueue(newEvent);

                if (WaitingRiders.Count == 1)
                    OnRiderWaiting?.Invoke(this, new WaitingRiderEventArgs(newEvent));
            }
        }

        private void OnEndId(RiderIdEventArgs args)
        {
            lock (EndLock)
            {
                LeftEvent newEvent = new LeftEvent(args.Received, args.RiderName, args.SensorId);

                //if we receive an end id for a rider that is not on track ignore it
                if (!OnTrackRiders.Any(t => t.id.Rider == newEvent.Rider))
                    return;

                RaceState.Enqueue(newEvent);

                TimingEvent closest = EndTimes.FirstOrDefault();

                //If the range on the id unit is larger than the stop box, we may receive an id event before a timing event
                if (closest == null)
                {
                    EndIds.Add(newEvent);
                    return;
                }

                foreach (TimingEvent e in EndTimes)
                    if ((e.Time - args.Received).Duration() < (closest.Time - args.Received).Duration())
                        closest = e;

                if ((closest.Time - args.Received).Duration().TotalSeconds <= 10)
                {
                    closest.SetRider(newEvent.Rider);
                    EndTimes.Remove(closest);

                    MatchLapEnd(newEvent, closest);
                }
                else
                    EndIds.Add(newEvent);
            }
        }

        private void OnTimer(TimingTriggeredEventArgs args)
        {
            //When a waiting rider triggers the start timing unit, they are recorded as on track
            if(args.GateId == TimingStartId)
            {
                lock (WaitingLock)
                {
                    bool hasWaitingRider = WaitingRiders.TryDequeue(out EnteredEvent rider);

                    if (hasWaitingRider)
                    {
                        TimingEvent newEvent = new TimingEvent(args.Received, rider.Rider, args.Microseconds, args.GateId);

                        OnTrackRiders.Enqueue((rider, newEvent));
                        RaceState.Enqueue(newEvent);

                        WaitingRiders.TryPeek(out EnteredEvent waiting);
                        if (waiting != null)
                            OnRiderWaiting?.Invoke(this, new WaitingRiderEventArgs(waiting));
                        else
                            OnStartEmpty?.Invoke(this, EventArgs.Empty);
                    }
                    //if we dont have a waiting rider, disregard event somebody probably walked through the beam
                }
            }
            //when a rider triggers the end timing unit, that must be matched to an end id unit event
            //if there is such a match, then it must be matched to an on track rider
            else if(args.GateId == TimingEndId)
            {
                lock (EndLock)
                {
                    //we dont know the rider yet
                    TimingEvent newEvent = new TimingEvent(args.Received, null, args.Microseconds, args.GateId);
                    RaceState.Enqueue(newEvent);

                    LeftEvent closest = EndIds.FirstOrDefault();

                    //if the rider id unit's range is smaller than the stop box, we may receive the rider id later
                    if (closest == null)
                    {
                        EndTimes.Add(newEvent);
                        return;
                    }

                    foreach (LeftEvent e in EndIds)
                        if ((e.Time - args.Received).Duration() < (closest.Time - args.Received).Duration())
                            closest = e;

                    if ((closest.Time - args.Received).Duration().TotalSeconds <= 10)
                    {
                        newEvent.SetRider(closest.Rider);
                        EndIds.Remove(closest);

                        MatchLapEnd(closest, newEvent);
                    }
                    else
                        EndTimes.Add(newEvent);
                }
            }
            else
            {
                throw new ArgumentException($"Found unkown gate id in timing event: {args.GateId}. Known gates are start: {TimingStartId}, end: {TimingEndId}");
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
            while (OnTrackRiders.TryDequeue(out (EnteredEvent startId, TimingEvent startTime) onTrack))
            {
                if(onTrack.startId.Rider == endId.Rider)
                {
                    finish = new FinishedEvent(onTrack.startId, onTrack.startTime, endTime, endId);
                    RaceState.Enqueue(finish);
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

            foreach((EnteredEvent startId, TimingEvent startTime) in dnf)
            {
                RaceState.Enqueue(new DNFEvent(finish, startId));
            }

            //filter out all older events that can never be matched
            //if an event is more than 10 seconds older than its most recently finished counterpart it will never be matched
            EndIds = EndIds.Where(e => (e.Time - finish.TimeEnd.Time).TotalSeconds > -10).ToList();
            EndTimes = EndTimes.Where(e => (e.Time - finish.Left.Time).TotalSeconds > -10).ToList();
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
