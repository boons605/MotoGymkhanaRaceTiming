﻿using DisplayUnit;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SensorUnits.TimingUnit
{
    public class ReplayTimingUnit : Simulation.BaseReplayUnit<TimingEvent>, ITimingUnit, IDisplayUnit
    {
        public event EventHandler<TimingTriggeredEventArgs> OnTrigger;

        public int StartId { get; private set; }
        public int EndId { get; private set; }

        public int CurrentDisplay { get; private set; }

        public ReplayTimingUnit(RaceSummary race)
            : base(race)
        {
            FinishedEvent finish = race.Events.Find(r => r is FinishedEvent) as FinishedEvent;

            StartId = finish.TimeStart.GateId;
            EndId = finish.TimeEnd.GateId;
        }

        public override void Initialize()
        {
            eventsToReplay = new Queue<TimingEvent>(race.Events.Where(r => r is TimingEvent).Select(r => r as TimingEvent));
        }

        protected override void Replay(TimingEvent raceEvent)
        {
            OnTrigger?.Invoke(this, new TimingTriggeredEventArgs(raceEvent.Microseconds, "ReplayTimer", raceEvent.GateId, raceEvent.Time));
        }

        public void SetDisplayTime(int milliSeconds)
        {
            CurrentDisplay = milliSeconds;
        }
    }
}