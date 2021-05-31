// <copyright file="RaceTracker.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Models;
using Models.Config;
using SensorUnits.RiderIdUnit;
using SensorUnits.TimingUnit;

namespace RaceManagement
{
    /// <summary>
    /// Class that keeps track of a race through events provided raised by timing and id units
    /// </summary>
    public class RaceTracker : IRaceTracker
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private TrackerConfig config;
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
        /// The events triggered by riders entering the start box. In FIFO order so the first rider to enter the box is the first to start
        /// </summary>
        private IndexedQueue<string, IdEvent> waitingRiders = new IndexedQueue<string, IdEvent>(id => id.Rider.Name);

        /// <summary>
        /// The matched id and time events that indicate a driver has left the start box and is on track. in FIFO order so the first element in this queue represent the rider that entered the track first
        /// </summary>
        private IndexedQueue<string, (IdEvent id, TimingEvent timer)> onTrackRiders = new IndexedQueue<string, (IdEvent id, TimingEvent timer)>(tuple => tuple.id.Rider.Name);

        /// <summary>
        /// The complete list of events in chronological order
        /// </summary>
        private Queue<RaceEvent> raceState = new Queue<RaceEvent>();

        /// <summary>
        /// List of timing events picked up by the end timing gate that have not been matched to an id from <see cref="endIds"/>. Oldest first
        /// </summary>
        private List<TimingEvent> endTimes = new List<TimingEvent>();

        /// <summary>
        /// List of id events picked up by the end id unit that have not been matched to a time from <see cref="endTimes"/>. Oldest first
        /// </summary>
        private List<IdEvent> endIds = new List<IdEvent>();

        /// <summary>
        /// Events waiting to be processed by the main loop
        /// </summary>
        private ConcurrentQueue<EventArgs> toProcess = new ConcurrentQueue<EventArgs>();

        private List<Lap> laps = new List<Lap>();

        private Dictionary<string, DSQEvent> pendingDisqualifications = new Dictionary<string, DSQEvent>();
        private Dictionary<string, List<PenaltyEvent>> pendingPenalties = new Dictionary<string, List<PenaltyEvent>>();

        private List<Rider> knownRiders = new List<Rider>();

        /// <summary>
        /// Fired when the system is ready for the next rider to trigger the start timing gate
        /// </summary>
        public event EventHandler<WaitingRiderEventArgs> OnRiderWaiting;

        /// <summary>
        /// Fired when a rider's lap time is known
        /// </summary>
        public event EventHandler<FinishedRiderEventArgs> OnRiderFinished;

        public event EventHandler<FinishedRiderEventArgs> OnRiderDNF;

        /// <summary>
        /// Fired when the system has no riders waiting to start
        /// </summary>
        public event EventHandler OnStartEmpty;

        public RaceTracker(ITimingUnit timing, IRiderIdUnit startGate, IRiderIdUnit endGate, TrackerConfig config, List<Rider> knownRiders)
        {
            this.timing = timing;
            this.startGate = startGate;
            this.endGate = endGate;
            this.knownRiders = knownRiders;
            this.startGate.AddKnownRiders(knownRiders);
            this.config = config;

            foreach(Rider rider in knownRiders)
            {
                pendingPenalties.Add(rider.Name, new List<PenaltyEvent>());
            }
        }

        /// <summary>
        /// Gives you an overview of the current state of the race
        /// Do not modify the returned objects
        /// </summary>
        public (List<IdEvent> waiting, List<(IdEvent id, TimingEvent timer)> onTrack, List<IdEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) GetState =>
            (waitingRiders.ToList(), onTrackRiders.ToList(), endIds.ToList(), endTimes.ToList());

        /// <summary>
        /// Returns a list of all laps driven so far
        /// </summary>
        public List<Lap> Laps => laps.ToList();

        /// <summary>
        /// Run a task that communicates with the timing and rider units to track the state of a race
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<RaceSummary> Run(CancellationToken token)
        {
            RegisterEvents();

            return Task.Run(() =>
            {
                while (!(token.IsCancellationRequested && toProcess.Count == 0))
                {
                    if (toProcess.TryDequeue(out EventArgs e))
                    {
                        switch (e)
                        {
                            case RiderIdEventArgs rider when rider.UnitId == startGate.UnitId:
                                OnStartId(rider);
                                break;
                            case RiderIdEventArgs rider when rider.UnitId == endGate.UnitId:
                                OnEndId(rider);
                                break;
                            case TimingTriggeredEventArgs time:
                                OnTimer(time);
                                break;
                            case PenaltyEventArgs penalty:
                                OnPenalty(penalty);
                                break;
                            case DSQEventArgs dsq:
                                OnDSQ(dsq);
                                break;
                            case ManualDNFEventArgs dnf:
                                OnManualDNF(dnf);
                                break;
                            default:
                                throw new ArgumentException($"Unknown event type: {e.GetType()}");
                        }
                    }
                }

                return new RaceSummary(raceState.ToList(), config, startGate.UnitId, endGate.UnitId);
            });
        }

        private void RegisterEvents()
        {
            timing.OnTrigger += (_, args) => OnEvent(args);
            startGate.OnRiderId += (_, args) => OnEvent(args);
            endGate.OnRiderId += (_, args) => OnEvent(args);
            startGate.OnRiderExit += (_, args) => OnEvent(args);
            endGate.OnRiderExit += (_, args) => OnEvent(args);
        }

        private void OnEvent(EventArgs e) => toProcess.Enqueue(e);


        /// <summary>
        /// When a rider enters the start box they are recorded as waiting to start.
        /// The next time the start timing is triggered, the oldest waiting rider will be recorded as on track
        /// </summary>
        /// <param name="args"></param>
        private void OnStartId(RiderIdEventArgs args)
        {
            //Is the rider associated with this even currently registered as waiting?
            bool waiting = waitingRiders.Any(e => e.Rider == args.Rider);

            //Is the rider associated with this event currently on track
            bool onTrack = onTrackRiders.Any(t => t.id.Rider == args.Rider);

            if (args.IdType == Direction.Enter)
            {
                if (!waiting && !onTrack)
                {
                    IdEvent newEvent = new IdEvent(args.Received, args.Rider, args.UnitId, args.IdType);
                    waitingRiders.Enqueue(newEvent);
                    raceState.Enqueue(newEvent);

                    if (waitingRiders.Count == 1)
                    {
                        OnRiderWaiting?.Invoke(this, new WaitingRiderEventArgs(newEvent));
                    }
                }
            }
            else
            {
                if(waiting)
                {
                    Log.Info($"Removing rider {args.Rider} from waiting list.");
                    waitingRiders.Remove(args.Rider.Name);

                    if(waitingRiders.Count == 0)
                    {
                        OnStartEmpty?.Invoke(this, EventArgs.Empty);
                    }
                }

                if(onTrack)
                {
                    Log.Info($"Removing on track rider {args.Rider} from start device.");
                    startGate.RemoveKnownRider(args.Rider.Name);
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
            if (args.IdType == Direction.Enter)
            {
                IdEvent newEvent = new IdEvent(args.Received, args.Rider, args.UnitId, args.IdType);

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

                if ((closest.Time - args.Received).Duration().TotalSeconds <= config.EndMatchTimeout)
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
            if (args.GateId == config.StartTimingGateId)
            {
                bool hasWaitingRider = waitingRiders.Count > 0;

                if (hasWaitingRider)
                {
                    IdEvent rider = waitingRiders.Dequeue();

                    TimingEvent newEvent = new TimingEvent(args.Received, rider.Rider, args.Microseconds, args.GateId);

                    onTrackRiders.Enqueue((rider, newEvent));
                    raceState.Enqueue(newEvent);

                    startGate.RemoveKnownRider(rider.Rider.Name);
                    endGate.AddKnownRiders(new List<Rider> { rider.Rider });

                    if (waitingRiders.Count > 0)
                    {
                        IdEvent waiting = waitingRiders.First();
                        OnRiderWaiting?.Invoke(this, new WaitingRiderEventArgs(waiting));
                    }
                    else
                    {
                        OnStartEmpty?.Invoke(this, EventArgs.Empty);
                    }
                } 
                else
                {
                    Log.Info($"Discarding timestamp from gate {args.GateId} at {args.Microseconds} us");
                }
            }
            else if (args.GateId == config.EndTimingGateId)
            {
                //when a rider triggers the end timing unit, that must be matched to an end id unit event
                //if there is such a match, then it must be matched to an on track rider

                //we dont know the rider yet
                TimingEvent newEvent = new TimingEvent(args.Received, null, args.Microseconds, args.GateId);
                raceState.Enqueue(newEvent);

                IdEvent closest = endIds.FirstOrDefault();

                //if the rider id unit's range is smaller than the stop box, we may receive the rider id later
                if (closest == null)
                {
                    endTimes.Add(newEvent);
                    return;
                }

                foreach (IdEvent e in endIds)
                {
                    if ((e.Time - args.Received).Duration() < (closest.Time - args.Received).Duration())
                    {
                        closest = e;
                    }
                }

                if ((closest.Time - args.Received).Duration().TotalSeconds <= config.EndMatchTimeout)
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
            else
            {
                throw new ArgumentException($"Found unkown gate id in timing event: {args.GateId}. Known gates are start: {config.StartTimingGateId}, end: {config.EndTimingGateId}");
            }
        }

        private void OnManualDNF(ManualDNFEventArgs raceEvent)
        {
            if (onTrackRiders.Contains(raceEvent.RiderName))
            {
                var onTrack = onTrackRiders.Remove(raceEvent.RiderName);

                Log.Info($"Received DNF event for rider {raceEvent.RiderName}");

                ManualDNFEvent dnf = new ManualDNFEvent(onTrack.id, raceEvent.StaffName);

                raceState.Enqueue(dnf);

                endGate.RemoveKnownRider(onTrack.id.Rider.Name);

                Lap lap = new Lap(dnf);
                this.laps.Add(lap);
                ApplyPendingEvents(lap);
                OnRiderDNF?.Invoke(this, new FinishedRiderEventArgs(lap));
            }
            else
            {
                ManualDNFEvent dnf = new ManualDNFEvent(new IdEvent(new DateTime(0), null, null, Direction.Enter), raceEvent.StaffName);

                raceState.Enqueue(dnf);

                Log.Info($"Received DNF event for rider {raceEvent.RiderName} who is not on track. Event will be ignored");
            }
        }

        /// <summary>
        /// Will register a DSQ event.
        /// If the rider is on track the DSQ event will be applied when the lap finishes (normally or DNF)
        /// If the rider is not on track, but has a completed lap the DSQ will be applied to the last completed lap
        /// If the rider is not on track and has no completed laps the event is ignored
        /// </summary>
        /// <param name="raceEvent"></param>
        private void OnDSQ(DSQEventArgs raceEvent)
        {
            Rider rider = knownRiders.Where(r => r.Name == raceEvent.RiderName).FirstOrDefault();

            DSQEvent dsq = new DSQEvent(raceEvent.Received, rider, raceEvent.StaffName, raceEvent.Reason);

            raceState.Enqueue(dsq);

            if (rider == null)
            {
                Log.Info($"Received DSQ event for unkown rider {raceEvent.RiderName}. Event will be ignored");
            }
            else
            {
                if (onTrackRiders.Contains(rider.Name))
                {
                    pendingDisqualifications.Add(rider.Name, dsq);
                }
                else
                {
                    Lap lastLap = laps.FindLast(l => l.Rider == rider && !l.Disqualified);

                    if (lastLap == null)
                    {
                        Log.Info($"Received DSQ event for rider {rider} that is not on track and has no laps without a DSQ this session. Event will be ignored");
                    }
                    else
                    {
                        lastLap.SetDsq(dsq);
                    }
                }
            }
        }

        /// <summary>
        /// Will register a penalty event
        /// If the rider is on track the Penalty will be applied when the lap is finshed (normally or DNF)
        /// If the rider is not on track, but has a completed lap the penalty will be applied to the last completed lap
        /// If the rider is not on track and has no completed laps the event is ignored
        /// </summary>
        /// <param name="raceEvent"></param>
        private void OnPenalty(PenaltyEventArgs raceEvent)
        {
            Rider rider = knownRiders.Where(r => r.Name == raceEvent.RiderName).FirstOrDefault();

            PenaltyEvent penalty = new PenaltyEvent(raceEvent.Received, rider, raceEvent.Reason, raceEvent.Seconds, raceEvent.StaffName);

            raceState.Enqueue(penalty);

            if (rider == null)
            {
                Log.Info($"Received Penalty event for unkown rider {raceEvent.RiderName}. Event will be ignored");
            }
            else
            {
                if (onTrackRiders.Contains(rider.Name))
                {
                    pendingPenalties[rider.Name].Add(penalty);
                }
                else
                {
                    Lap lastLap = laps.FindLast(l => l.Rider == rider);

                    if (lastLap == null)
                    {
                        Log.Info($"Received Penalty event for rider {rider} that is not on track and has no laps this session. Event will be ignored");
                    }
                    else
                    {
                        lastLap.AddPenalties(new List<PenaltyEvent> { penalty });
                    }
                }
            }
        }

        /// <summary>
        /// Applies any panding DSQ and Penalty events to a completed lap
        /// </summary>
        /// <param name="lap"></param>
        private void ApplyPendingEvents(Lap lap)
        {
            if(pendingDisqualifications.ContainsKey(lap.Rider.Name))
            {
                lap.SetDsq(pendingDisqualifications[lap.Rider.Name]);
                pendingDisqualifications.Remove(lap.Rider.Name);
            }

            lap.AddPenalties(pendingPenalties[lap.Rider.Name]);
            pendingPenalties[lap.Rider.Name].Clear();
        }

        public void AddRider(Rider rider)
        {
            startGate.AddKnownRiders(new List<Rider> { rider });
            knownRiders.Add(rider);
            pendingPenalties.Add(rider.Name, new List<PenaltyEvent>());
        }

        public void RemoveRider(string name)
        {
            endGate.RemoveKnownRider(name);
            startGate.RemoveKnownRider(name);

            knownRiders.RemoveAll(r => r.Name == name);
            pendingDisqualifications.Remove(name);
            pendingPenalties.Remove(name);
        }

        /// <summary>
        /// This method tries to match a lap end, with an on track rider
        /// if the lap end matches the oldest on track rider, this rider gets a Finished event
        /// if the lap end matches any younger rider, that rider gets a Finished event and any older on track riders get a DNF event
        /// </summary>
        /// <param name="id"></param>
        /// <param name="time"></param>
        private void MatchLapEnd(IdEvent endId, TimingEvent endTime)
        {
            List<(IdEvent startId, TimingEvent startTime)> dnf = new List<(IdEvent startId, TimingEvent startTime)>();

            FinishedEvent finish = null;
            while (onTrackRiders.Count > 0)
            {
                (IdEvent startId, TimingEvent startTime) onTrack = onTrackRiders.Dequeue();

                if (onTrack.startId.Rider == endId.Rider)
                {
                    finish = new FinishedEvent(onTrack.startId, onTrack.startTime, endTime, endId);
                    raceState.Enqueue(finish);

                    Lap lap = new Lap(finish);

                    laps.Add(lap);
                    ApplyPendingEvents(lap);
                    OnRiderFinished?.Invoke(this, new FinishedRiderEventArgs(lap));

                    endGate.RemoveKnownRider(endId.Rider.Name);
                    startGate.AddKnownRiders(new List<Rider> { endId.Rider });

                    break;
                }
                else
                {
                    //if there are older riders that do not match, they must have left the track without passing the stop box
                    //since they are older they appear earlier in the loop
                    dnf.Add(onTrack);
                }
            }

            foreach ((IdEvent startId, TimingEvent startTime) in dnf)
            {
                UnitDNFEvent dnfEvent = new UnitDNFEvent(finish, startId);
                raceState.Enqueue(dnfEvent);

                Lap lap = new Lap(dnfEvent);

                laps.Add(lap);
                ApplyPendingEvents(lap);
                OnRiderDNF?.Invoke(this, new FinishedRiderEventArgs(lap));

                endGate.RemoveKnownRider(dnfEvent.Rider.Name);
                startGate.AddKnownRiders(new List<Rider> { dnfEvent.Rider });
            }

            //filter out all older events that can never be matched
            //if an event is more than 10 seconds older than its most recently finished counterpart it will never be matched
            endIds = endIds.Where(e => (e.Time - finish.TimeEnd.Time).TotalSeconds > -config.EndMatchTimeout).ToList();
            endTimes = endTimes.Where(e => (e.Time - finish.Left.Time).TotalSeconds > -config.EndMatchTimeout).ToList();
        }

        public void AddEvent<T>(T manualEvent) where T : ManualEventArgs
        {
            OnEvent(manualEvent);
        }
    }

    public class FinishedRiderEventArgs : EventArgs
    {
        public Lap Lap { get; private set; }

        public FinishedRiderEventArgs(Lap lap)
        {
            Lap = lap;
        }
    }

    public class WaitingRiderEventArgs : EventArgs
    {
        public IdEvent Rider { get; private set; }

        public WaitingRiderEventArgs(IdEvent rider)
        {
            Rider = rider;
        }
    }
}
