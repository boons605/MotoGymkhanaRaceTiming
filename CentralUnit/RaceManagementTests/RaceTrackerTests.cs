// <copyright file="RaceTrackerTests.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Models.Config;
using RaceManagement;
using RaceManagementTests.TestHelpers;

namespace RaceManagementTests
{
    [TestClass]
    public class RaceTrackerTests
    {
        MockStartLightUnit lightUnit;
        MockTimingUnit timer;

        CancellationTokenSource source;

        RaceTracker subject;

        Task<RaceSummary> race;

        [TestInitialize]
        public void Init()
        {
            lightUnit = new MockStartLightUnit();
            timer = new MockTimingUnit();

            source = new CancellationTokenSource();

            TrackerConfig config = new TrackerConfig
            {
                StartTimingGateId = 0,
                EndTimingGateId = 1
            };

            subject = new RaceTracker(timer, config, new List<Rider> { });

            subject.OnRiderWaiting += (o, e) => lightUnit.SetStartLightColor(SensorUnits.StartLightUnit.StartLightColor.GREEN);
            subject.OnStartEmpty += (o, e) => lightUnit.SetStartLightColor(SensorUnits.StartLightUnit.StartLightColor.RED);

            race = subject.Run(source.Token);
        }

        [TestMethod]
        public void OnRiderReady_ShouldSaveEvent()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            source.Cancel();

            RaceSummary summary = race.Result;
            (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) = subject.GetState;

            Assert.AreEqual(1, summary.Events.Count);

            RiderReadyEvent entered = summary.Events[0] as RiderReadyEvent;
            Assert.AreEqual(entered, waiting);

            Assert.AreEqual("Martijn", entered.Rider.Name);
            Assert.AreEqual(martijn.Id, entered.Rider.Id);
            Assert.AreEqual(new DateTime(2000, 1, 1, 1, 1, 1), entered.Time);
        }

        [TestMethod]
        public void OnTimer_ForStart_WithoutWaitingRider_ShouldIgnoreEvent()
        {
            //0 is the start gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();

            RaceSummary summary = race.Result;
            (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) = subject.GetState;

            Assert.AreEqual(0, summary.Events.Count);
            Assert.AreEqual(0, onTrack.Count);
            Assert.IsNull(waiting);
        }

        [TestMethod]
        public void OnTimer_ForStart_WitWaitingRider_ShouldMatchWithRider()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            //rider enters start box
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();

            RaceSummary summary = race.Result;
            (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) = subject.GetState;

            //we expect an EnteredEvent and a TimingEvent, in that order
            Assert.AreEqual(2, summary.Events.Count);

            RiderReadyEvent ready = summary.Events[0] as RiderReadyEvent;
            TimingEvent start = summary.Events[1] as TimingEvent;
            Assert.AreEqual(onTrack[0].rider, ready);
            Assert.AreEqual(onTrack[0].timer, start);

            Assert.AreEqual("Martijn", start.Rider.Name);
            Assert.AreEqual(100L, start.Microseconds);
            Assert.AreEqual(new DateTime(2000, 1, 1, 1, 1, 1), start.Time);

            Assert.IsNull(waiting);
            Assert.AreEqual(0, unmatchedTimes.Count);
        }

        [TestMethod]
        public void OnTimer_ForEnd_WithoutRiderLeft_ShouldSaveEvent()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            MakeStartEvents(martijn, DateTime.Now, timer, subject, 200);

            DateTime endTime = DateTime.Now;
            timer.EmitTriggerEvent(200, "Timer", 1, endTime);

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            // since the user has to manually match an end timing event for a rider the timing event will always come in first and have to be saved
            Assert.AreEqual(3, summary.Events.Count);

            //the state should match the summary
            TimingEvent end = summary.Events[2] as TimingEvent;
            Assert.AreEqual(state.unmatchedTimes[0], end);

            Assert.AreEqual(null, end.Rider); //a lone timer event cannot have a rider
            Assert.AreEqual(200, end.Microseconds);
            Assert.AreEqual(endTime, end.Time);
        }

        [TestMethod]
        public void OnTimer_ForEnd_WithoutRiderOnTRack_ShouldIgnoreEvent()
        {
            //1 is the end gate
            timer.EmitTriggerEvent(100, "Timer", 1, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            // since the user has to manually match an end timing event for a rider the timing event will always come in first and have to be saved
            Assert.AreEqual(0, summary.Events.Count);
            Assert.AreEqual(0, state.unmatchedTimes.Count);
        }

        [TestMethod]
        public void OnRiderFinished_WithoutRiderOnTrack_ShouldIgnoreEvent()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());

            subject.AddEvent(new RiderFinishedEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff", Guid.NewGuid()));

            source.Cancel();

            RaceSummary summary = race.Result;
            (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) = subject.GetState;

            Assert.AreEqual(0, summary.Events.Count);
            Assert.AreEqual(0, onTrack.Count);
            Assert.AreEqual(0, unmatchedTimes.Count);
            Assert.IsNull(waiting);
        }

        [TestMethod]
        public void OnEndId_WithDifferentRiderOnTrack_ShouldIgnoreEvent()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider richard = new Rider("Richard", Guid.NewGuid());
            subject.AddRider(richard);

            //rider enters start box
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            //rider triggers start timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 1));

            // rider triggers end timing gate
            timer.EmitTriggerEvent(100, "Timer", 1, new DateTime(2000, 1, 1, 1, 1, 1));

            // wait for tracker to process events
            while (subject.GetState.unmatchedTimes.Count == 0)
            {
                Thread.Yield();
            }

            //we need to know the id of the end timing event
            Guid timingId = subject.GetState.unmatchedTimes[0].EventId;

            //rider not on track triggers end id, should not be recorded
            subject.AddEvent(new RiderFinishedEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), richard.Id, "staff", timingId));

            source.Cancel();

            RaceSummary summary = race.Result;
            (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) = subject.GetState;

            //we expect only the events for Martijn to be recorded
            Assert.AreEqual(3, summary.Events.Count);

            RiderReadyEvent ready = summary.Events[0] as RiderReadyEvent;
            TimingEvent start = summary.Events[1] as TimingEvent;
            Assert.AreEqual(onTrack[0].rider.Rider.Id, ready.Rider.Id);
            Assert.AreEqual(onTrack[0].timer, start);

            Assert.AreEqual("Martijn", start.Rider.Name);
            Assert.AreEqual(100L, start.Microseconds);
            Assert.AreEqual(new DateTime(2000, 1, 1, 1, 1, 1), start.Time);

            //no riders should be waiting at the start.
            //there is still the end timing event to be matched
            Assert.IsNull(waiting);
            Assert.AreEqual(1, unmatchedTimes.Count);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OnEndId_WithMatchingTiming_ShouldCompleteLap(bool includeUnmatchedTime)
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider richard = new Rider("Richard", Guid.NewGuid());
            subject.AddRider(richard);

            //rider enters start box
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 2));

            //somebody walks through end timing gate 10 secs after rider has started
            if (includeUnmatchedTime)
            {
                timer.EmitTriggerEvent(400, "Timer", 1, new DateTime(2000, 1, 1, 1, 1, 12));
            }

            // rider triggers end timing gate
            timer.EmitTriggerEvent(500, "Timer", 1, new DateTime(2000, 1, 1, 1, 1, 1));

            // wait for tracker to process events
            while (subject.GetState.unmatchedTimes.Count == 0)
            {
                Thread.Yield();
            }

            //we need to know the id of the end timing event
            Guid timingId = subject.GetState.unmatchedTimes.Last().EventId;

            subject.AddEvent(new RiderFinishedEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff", timingId));

            source.Cancel();
            (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) state = subject.GetState;

            FinishedEvent finish = race.Result.Events.Last() as FinishedEvent;

            //Martijn should have done a lightning fast 400 microsecond lap
            Assert.AreEqual("Martijn", finish.Rider.Name);
            Assert.AreEqual(400L, finish.LapTime);

            //There should be nothing going on in the race at this point, but there should still be an unmatched time
            Assert.IsNull(state.waiting);

            if (includeUnmatchedTime)
            {
                Assert.AreEqual(1, state.unmatchedTimes.Count);
            }
            else
            {
                Assert.AreEqual(0, state.unmatchedTimes.Count);
            }

            Assert.AreEqual(0, state.onTrack.Count);

            // make sure the unmacthed time is left
            if (includeUnmatchedTime)
            {
                Assert.AreNotEqual(timingId, state.unmatchedTimes[0].EventId);
            }
            else
            {
                Assert.AreEqual(0, state.unmatchedTimes.Count);
            }
        }

        [TestMethod]
        public void OnDeleteTime_WithoutPendingTimes_ShouldBeIgnored()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            MakeStartEvents(martijn, DateTime.Now, timer, subject, 200);

            // there are no pending times so this should have no effect
            Guid targetId = Guid.NewGuid();
            DateTime eventTime = DateTime.Now;
            subject.AddEvent(new DeleteTimeEventArgs(eventTime, "staff", targetId));

            source.Cancel();
            DeleteTimeEvent delete = race.Result.Events.Last() as DeleteTimeEvent;

            (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) = subject.GetState;


            Assert.AreEqual(0, unmatchedTimes.Count);
            Assert.IsNull(waiting);
            Assert.AreEqual(1, onTrack.Count);

            Assert.AreEqual(targetId, delete.TargetEventId);
            Assert.IsNull(delete.Rider);
            Assert.AreNotEqual(Guid.Empty, delete.EventId);
        }

        [TestMethod]
        public void OnDeleteTime_WithWrongId_ShouldBeIgnored()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            MakeStartEvents(martijn, DateTime.Now, timer, subject, 200);
            timer.EmitTriggerEvent(201, "Timer", 1, DateTime.Now);

            // wait for tracker to process events
            while (subject.GetState.unmatchedTimes.Count != 1)
            {
                Thread.Yield();
            }

            Guid timeId = subject.GetState.unmatchedTimes[0].EventId;
            Guid wrongId = Guid.NewGuid();

            // astronomically unlikely we have the same guid twice but lets be safe
            while (timeId == wrongId)
            {
                wrongId = Guid.NewGuid();
            }

            DateTime eventTime = DateTime.Now;
            subject.AddEvent(new DeleteTimeEventArgs(eventTime, "staff", wrongId));

            source.Cancel();
            DeleteTimeEvent delete = race.Result.Events.Last() as DeleteTimeEvent;

            (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) = subject.GetState;

            Assert.AreEqual(1, unmatchedTimes.Count);
            Assert.IsNull(waiting);
            Assert.AreEqual(1, onTrack.Count);

            Assert.AreEqual(wrongId, delete.TargetEventId);
            Assert.IsNull(delete.Rider);
            Assert.AreNotEqual(Guid.Empty, delete.EventId);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void OnDeleteTime_WithCorrectId_ShouldDeleteEvent(int eventIndex)
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            MakeStartEvents(martijn, DateTime.Now, timer, subject, 0);
            timer.EmitTriggerEvent(0, "Timer", 1, DateTime.Now);
            timer.EmitTriggerEvent(1, "Timer", 1, DateTime.Now);

            // wait for tracker to process events
            while (subject.GetState.unmatchedTimes.Count != 2)
            {
                Thread.Yield();
            }

            Guid targetId = subject.GetState.unmatchedTimes.First(e => e.Microseconds == eventIndex).EventId;

            subject.AddEvent(new DeleteTimeEventArgs(DateTime.Now, "staff", targetId));

            while (subject.GetState.unmatchedTimes.Count != 1)
            {
                Thread.Yield();
            }

            // make sure the right event got deleted
            Assert.AreNotEqual(eventIndex, subject.GetState.unmatchedTimes[0].Microseconds);

            MakeEndEvents(martijn, DateTime.Now, timer, subject, 2);

            // wait for rider to finish
            while (subject.GetState.onTrack.Count != 0)
            {
                Thread.Yield();
            }

            // make sure the remaining time event is the one we did not delete
            Assert.AreNotEqual(eventIndex, subject.GetState.unmatchedTimes[0].Microseconds);

            //also delete that one
            Guid lastTarget = subject.GetState.unmatchedTimes[0].EventId;
            subject.AddEvent(new DeleteTimeEventArgs(DateTime.Now, "staff", lastTarget));

            source.Cancel();
            Assert.AreEqual(2, race.Result.Events.Where(e => e is DeleteTimeEvent).Count());
            Assert.AreEqual(0, subject.GetState.unmatchedTimes.Count);
        }

        [TestMethod]
        public void OnRiderReady_WithWaitingRider_ShouldbeIgnored()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider bert = new Rider("Bert", Guid.NewGuid());
            subject.AddRider(bert);

            string waiting = null;

            subject.OnRiderWaiting += (obj, args) => waiting = args.Rider.Rider.Name;

            //rider enters start box
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            //the race tracker is processing events in a different thread, we must wait for it
            Thread.Sleep(1000);

            //Martijn should be flagged as ready to start
            Assert.AreEqual(waiting, "Martijn");

            //Second rider enters the queue to start
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 2, 1), bert.Id, "staff"));

            //the race tracker is processing events in a different thread, we must wait for it
            Thread.Sleep(1000);

            //OnRiderWaiting should not be fired since Martijn has not left
            Assert.AreEqual(waiting, "Martijn");

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            source.Cancel();
            race.Wait();
        }

        [TestMethod]
        public void OnStartEmpty_WhenRiderStart_ShouldFire()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider bert = new Rider("Bert", Guid.NewGuid());
            subject.AddRider(bert);

            bool isEmpty = false;

            subject.OnStartEmpty += (obj, args) => isEmpty = true;

            //rider enters start box
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            //the race tracker is processing events in a different thread, we must wait for it
            Thread.Sleep(1000);

            //event should not have fired
            Assert.IsFalse(isEmpty);

            //event should not have fired
            Assert.IsFalse(isEmpty);

            //Martijn leaves start box
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            source.Cancel();
            race.Wait();

            Assert.IsTrue(isEmpty);
        }

        [TestMethod]
        public void OnRiderFinished_WithFinishAndDNF_ShouldFireForFinish()
        {
            List<string> finished = new List<string>();

            subject.OnRiderMatched += (obj, args) => finished.Add(args.Lap.Rider.Name);

            SimulateRaceWithDNF();

            source.Cancel();
            race.Wait();

            Assert.AreEqual(2, finished.Count);
            Assert.AreEqual("Martijn", finished[0]);
            Assert.AreEqual("Bert", finished[1]);
        }

        [TestMethod]
        public void ManualDnf_WithRiderOnTrack_ShouldEndLap()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            //rider enters start box
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            subject.AddEvent(new ManualDNFEventArgs(DateTime.Now, martijn.Id, "staff"));

            source.Cancel();
            race.Wait();

            Assert.AreEqual(1, subject.Laps.Count);

            Lap lap = subject.Laps[0];

            Assert.IsTrue(lap.End is ManualDNFEvent);
            Assert.AreEqual(-1, lap.GetLapTime());
            Assert.IsFalse(lap.Disqualified);
        }

        [TestMethod]
        public void ManualDnf_WithoutRiderOnTrack_ShouldBeIgnored()
        {
            subject.AddEvent(new ManualDNFEventArgs(DateTime.Now, Guid.NewGuid(), "staff"));

            source.Cancel();
            race.Wait();

            Assert.AreEqual(0, subject.Laps.Count);
        }

        [TestMethod]
        public void ManualDnf_WithDifferentRiderOnTrack_ShouldBeIgnored()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider bert = new Rider("Bert", Guid.NewGuid());
            subject.AddRider(bert);

            //rider enters start box
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            subject.AddEvent(new ManualDNFEventArgs(DateTime.Now, bert.Id, "staff"));

            source.Cancel();
            race.Wait();

            Assert.AreEqual(0, subject.Laps.Count);
        }

        [TestMethod]
        public void ManualDnf_WithMultipleRidersOnTrack_ShouldPickCorrect()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider bert = new Rider("Bert", Guid.NewGuid());
            subject.AddRider(bert);

            //rider enters start box
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 2), bert.Id, "staff"));
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 2));

            subject.AddEvent(new ManualDNFEventArgs(DateTime.Now, bert.Id, "staff"));

            source.Cancel();
            race.Wait();

            Assert.AreEqual(1, subject.Laps.Count);

            Lap lap = subject.Laps[0];

            Assert.IsTrue(lap.End is ManualDNFEvent);
            Assert.AreEqual(-1, lap.GetLapTime());
            Assert.IsFalse(lap.Disqualified);
            Assert.AreEqual(bert, lap.Rider);
        }

        [TestMethod]
        public void Penalty_WithoutRiderOnTrack_ShouldBeIgnored()
        {
            subject.AddEvent(new PenaltyEventArgs(DateTime.Now, Guid.NewGuid(), "staff", "test", 1));

            source.Cancel();
            RaceSummary summary = race.Result;

            //The penalty event is still recorded, but its not applied to any lap
            Assert.AreEqual(1, summary.Events.Count);
            PenaltyEvent penalty = summary.Events[0] as PenaltyEvent;
            Assert.AreEqual("unknown", penalty.Rider.Name);
            Assert.AreEqual("test", penalty.Reason);
            Assert.AreEqual("staff", penalty.StaffName);
            Assert.AreEqual(1, penalty.Seconds);
            Assert.AreEqual(0, subject.PendingPenalties.Count);
        }

        [TestMethod]
        public void Penalty_WithRiderOnTrack_ShouldApplyOnFinish()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            //rider enters start box
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            subject.AddEvent(new PenaltyEventArgs(DateTime.Now, martijn.Id, "staff", "testEvent", 1));
            subject.AddEvent(new PenaltyEventArgs(DateTime.Now, martijn.Id, "staff", "testEvent", 2));

            //finish the lap
            timer.EmitTriggerEvent(200, "Timer", 1, new DateTime(2000, 1, 1, 1, 2, 1));

            while (subject.GetState.unmatchedTimes.Count == 0)
            {
                Thread.Yield();
            }

            Guid timingId = subject.GetState.unmatchedTimes[0].EventId;
            subject.AddEvent(new RiderFinishedEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff", timingId));


            //do another lap, this one should not have any penalties
            MakeStartEvents(martijn, DateTime.Now, timer, subject);
            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            source.Cancel();
            race.Wait();

            Assert.AreEqual(2, subject.Laps.Count);
            Assert.AreEqual(1, subject.PendingPenalties.Count);
            Assert.AreEqual(0, subject.PendingPenalties[martijn.Id].Count);

            Lap penaltyLap = subject.Laps[0];

            Assert.IsTrue(penaltyLap.End is FinishedEvent);
            //100 micros lap time, 3 second penalty
            Assert.AreEqual(3000100, penaltyLap.GetLapTime());
            Assert.IsFalse(penaltyLap.Disqualified);

            Lap normalLap = subject.Laps[1];

            Assert.IsTrue(normalLap.End is FinishedEvent);
            //Lap from MakeEvent methods has nonsense lap time
            Assert.AreEqual(0, normalLap.GetLapTime());
            Assert.IsFalse(normalLap.Disqualified);
        }

        [TestMethod]
        public void Penalty_WithDifferentRiderOnTrack_ShouldBeIgnored()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider bert = new Rider("Bert", Guid.NewGuid());
            subject.AddRider(bert);

            //start a lap, for martijn
            MakeStartEvents(martijn, DateTime.Now, timer, subject);

            //somebody accidentally enters a penalty for bert
            subject.AddEvent(new PenaltyEventArgs(DateTime.Now, bert.Id, "staff", "test", 1));

            //martijn finishes
            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            //bert does a lap
            MakeStartEvents(bert, DateTime.Now, timer, subject);
            MakeEndEvents(bert, DateTime.Now, timer, subject);

            source.Cancel();
            race.Wait();

            Assert.AreEqual(2, subject.Laps.Count);
            Assert.AreEqual(2, subject.PendingPenalties.Count);
            Assert.AreEqual(0, subject.PendingPenalties[martijn.Id].Count);
            Assert.AreEqual(0, subject.PendingPenalties[bert.Id].Count);

            //neither lap should have a penalty
            foreach (Lap l in subject.Laps)
            {
                Assert.AreEqual(0, l.Penalties.Count);
            }
        }

        [TestMethod]
        public void Penalty_WithExistingLapAndNotOnTrack_ShouldApplyToLastLap()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider bert = new Rider("Bert", Guid.NewGuid());
            subject.AddRider(bert);

            MakeStartEvents(bert, DateTime.Now, timer, subject);
            MakeEndEvents(bert, DateTime.Now, timer, subject);

            MakeStartEvents(martijn, DateTime.Now, timer, subject);
            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            MakeStartEvents(martijn, DateTime.Now, timer, subject);
            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            subject.AddEvent(new PenaltyEventArgs(DateTime.Now, bert.Id, "staff", "test", 1));
            subject.AddEvent(new PenaltyEventArgs(DateTime.Now, martijn.Id, "staff", "test", 1));


            source.Cancel();
            race.Wait();

            Assert.AreEqual(3, subject.Laps.Count);
            Assert.AreEqual(2, subject.PendingPenalties.Count);
            Assert.AreEqual(0, subject.PendingPenalties[martijn.Id].Count);
            Assert.AreEqual(0, subject.PendingPenalties[bert.Id].Count);

            //Bert has only one lap, so all penalties should land there
            Assert.AreEqual(1, subject.Laps[0].Penalties.Count);
            Assert.AreEqual(bert, subject.Laps[0].Penalties[0].Rider);

            //Martijn has 2 laps, penalties should only apply to the last one
            Assert.AreEqual(0, subject.Laps[1].Penalties.Count);

            Assert.AreEqual(1, subject.Laps[2].Penalties.Count);
            Assert.AreEqual(martijn, subject.Laps[2].Penalties[0].Rider);
        }

        [TestMethod]
        public void Penalty_WithExistingLapAndOnTrack_ShouldApplyOnFinish()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            MakeStartEvents(martijn, DateTime.Now, timer, subject);
            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            MakeStartEvents(martijn, DateTime.Now, timer, subject);

            subject.AddEvent(new PenaltyEventArgs(DateTime.Now, martijn.Id, "staff", "test", 1));

            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            source.Cancel();
            race.Wait();

            Assert.AreEqual(2, subject.Laps.Count);
            Assert.AreEqual(1, subject.PendingPenalties.Count);
            Assert.AreEqual(0, subject.PendingPenalties[martijn.Id].Count);

            //Martijn has 2 laps, penalties were sent when he was on track for lap 2.
            Assert.AreEqual(0, subject.Laps[0].Penalties.Count);

            Assert.AreEqual(1, subject.Laps[1].Penalties.Count);
            Assert.AreEqual(martijn, subject.Laps[1].Penalties[0].Rider);
        }

        [TestMethod]
        public void Penalty_WithRiderOnTrack_ShouldbeReturned()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            Rider bert = new Rider("Bert", Guid.NewGuid());
            subject.AddRider(martijn);
            subject.AddRider(bert);

            //rider enters start box
            subject.AddEvent(new RiderReadyEventArgs(new DateTime(2000, 1, 1, 1, 1, 1), martijn.Id, "staff"));

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            //these should be pending
            subject.AddEvent(new PenaltyEventArgs(DateTime.Now, martijn.Id, "staff", "testEvent", 1));
            subject.AddEvent(new PenaltyEventArgs(DateTime.Now, martijn.Id, "staff", "testEvent", 2));
            subject.AddEvent(new DSQEventArgs(DateTime.Now, martijn.Id, "staff", "testDSQ"));

            //these should be ignored since bert is not on tack
            subject.AddEvent(new PenaltyEventArgs(DateTime.Now, bert.Id, "staff", "testEvent", 1));
            subject.AddEvent(new DSQEventArgs(DateTime.Now, bert.Id, "staff", "testDSQ"));

            MakeStartEvents(bert, DateTime.Now, timer, subject);

            source.Cancel();
            race.Wait();

            Assert.AreEqual(0, subject.Laps.Count);

            Dictionary<Guid, List<ManualEvent>> penalties = subject.PendingPenalties;

            Assert.AreEqual(2, penalties.Count);
            Assert.AreEqual(3, penalties[martijn.Id].Count);
            Assert.AreEqual(1, (penalties[martijn.Id][0] as PenaltyEvent).Seconds);
            Assert.AreEqual(2, (penalties[martijn.Id][1] as PenaltyEvent).Seconds);
            Assert.IsTrue(penalties[martijn.Id][2] is DSQEvent);

            Assert.AreEqual(0, penalties[bert.Id].Count);
        }

        [TestMethod]
        public void DSQ_WithoutRiderOnTrack_ShouldBeIgnored()
        {
            subject.AddEvent(new DSQEventArgs(DateTime.Now, Guid.NewGuid(), "staff", "test"));

            source.Cancel();
            RaceSummary summary = race.Result;

            //The dsq event is still recorded, but its not applied to any lap
            Assert.AreEqual(1, summary.Events.Count);
            DSQEvent dsq = summary.Events[0] as DSQEvent;
            Assert.AreEqual("unknown", dsq.Rider.Name);
            Assert.AreEqual("test", dsq.Reason);
            Assert.AreEqual("staff", dsq.StaffName);
        }

        [TestMethod]
        public void DSQ_WithRiderOnTrack_ShouldApplyOnFinish()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            MakeStartEvents(martijn, new DateTime(2000, 1, 1), timer, subject, 100);

            subject.AddEvent(new DSQEventArgs(DateTime.Now, martijn.Id, "staff", "testEvent"));

            //finish the lap
            MakeEndEvents(martijn, new DateTime(2000, 1, 1, 1, 2, 1), timer, subject, 200);


            //do another lap, this one should not be disqualified
            MakeStartEvents(martijn, DateTime.Now, timer, subject, 300);
            MakeEndEvents(martijn, DateTime.Now, timer, subject, 500);

            source.Cancel();
            race.Wait();

            Assert.AreEqual(2, subject.Laps.Count);

            Lap dsqLap = subject.Laps[0];

            Assert.IsTrue(dsqLap.End is FinishedEvent);
            //dsq still have a laptime based on microseconds
            Assert.AreEqual(100, dsqLap.GetLapTime());
            Assert.IsTrue(dsqLap.Disqualified);

            Lap normalLap = subject.Laps[1];

            Assert.IsTrue(normalLap.End is FinishedEvent);
            Assert.AreEqual(200, normalLap.GetLapTime());
            Assert.IsFalse(normalLap.Disqualified);
        }

        [TestMethod]
        public void DSQ_WithDifferentRiderOnTrack_ShouldBeIgnored()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider bert = new Rider("Bert", Guid.NewGuid());
            subject.AddRider(bert);

            //start a lap, for martijn
            MakeStartEvents(martijn, DateTime.Now, timer, subject);

            //somebody accidentally enters a DSQ for bert
            subject.AddEvent(new DSQEventArgs(DateTime.Now, bert.Id, "staff", "test"));

            //martijn finishes
            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            //bert does a lap
            MakeStartEvents(bert, DateTime.Now, timer, subject);
            MakeEndEvents(bert, DateTime.Now, timer, subject);

            source.Cancel();
            race.Wait();

            Assert.AreEqual(2, subject.Laps.Count);

            //neither lap should be disqualified
            foreach (Lap l in subject.Laps)
            {
                Assert.IsFalse(l.Disqualified);
            }
        }

        [TestMethod]
        public void DSQ_WithExistingLapAndNotOnTrack_ShouldApplyToLastLap()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider bert = new Rider("Bert", Guid.NewGuid());
            subject.AddRider(bert);

            MakeStartEvents(bert, DateTime.Now, timer, subject);
            MakeEndEvents(bert, DateTime.Now, timer, subject);

            MakeStartEvents(martijn, DateTime.Now, timer, subject);
            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            MakeStartEvents(martijn, DateTime.Now, timer, subject);
            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            subject.AddEvent(new DSQEventArgs(DateTime.Now, bert.Id, "staff", "test"));
            subject.AddEvent(new DSQEventArgs(DateTime.Now, martijn.Id, "staff", "test"));


            source.Cancel();
            race.Wait();

            Assert.AreEqual(3, subject.Laps.Count);

            //Bert has only one lap, so that should be disqualified
            Assert.IsTrue(subject.Laps[0].Disqualified);
            Assert.AreEqual(bert, subject.Laps[0].Dsq.Rider);

            //Martijn has 2 laps, only the last one should be disqualified
            Assert.IsFalse(subject.Laps[1].Disqualified);

            Assert.IsTrue(subject.Laps[2].Disqualified);
            Assert.AreEqual(martijn, subject.Laps[2].Dsq.Rider);
        }

        [TestMethod]
        public void DSQ_WithExistingLapAndOnTrack_ShouldApplyOnFinish()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            MakeStartEvents(martijn, DateTime.Now, timer, subject);
            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            MakeStartEvents(martijn, DateTime.Now, timer, subject);

            subject.AddEvent(new DSQEventArgs(DateTime.Now, martijn.Id, "staff", "test"));

            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            source.Cancel();
            race.Wait();

            Assert.AreEqual(2, subject.Laps.Count);

            //Martijn has 2 laps, DSQ was sent while he was on track for the second
            Assert.IsFalse(subject.Laps[0].Disqualified);

            Assert.IsTrue(subject.Laps[1].Disqualified);
            Assert.AreEqual(martijn, subject.Laps[1].Dsq.Rider);
        }

        [TestMethod]
        public void DSQ_WithPendingDSQ_ShouldbeIgnored()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            MakeStartEvents(martijn, new DateTime(2000, 1, 1), timer, subject, 100);

            subject.AddEvent(new DSQEventArgs(DateTime.Now, martijn.Id, "staff", "testEvent"));
            subject.AddEvent(new DSQEventArgs(DateTime.Now, martijn.Id, "staff", "testEvent"));

            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            // drive on more lap, make sure the extra dsq does not get aplied
            MakeStartEvents(martijn, DateTime.Now, timer, subject);
            MakeEndEvents(martijn, DateTime.Now, timer, subject);

            source.Cancel();
            race.Wait();

            Assert.AreEqual(2, subject.Laps.Count);

            //Martijn has 2 laps, DSQ was sent while he was on track for the second
            Assert.IsTrue(subject.Laps[0].Disqualified);
            Assert.AreEqual(martijn, subject.Laps[0].Dsq.Rider);

            Assert.IsFalse(subject.Laps[1].Disqualified);
        }

        /// <summary>
        /// Simulates a race where Martijn, Richard and Bert start, but only Martijn and Bert finish
        /// </summary>
        private void SimulateRaceWithDNF()
        {
            DateTime start = new DateTime(2000, 1, 1, 1, 1, 1);

            Rider martijn = new Rider("Martijn", Guid.NewGuid());
            subject.AddRider(martijn);

            Rider richard = new Rider("Richard", Guid.NewGuid());
            subject.AddRider(richard);

            Rider bert = new Rider("Bert", Guid.NewGuid());
            subject.AddRider(bert);

            MakeStartEvents(martijn, start, timer, subject);

            start = start.AddSeconds(30);

            MakeStartEvents(richard, start, timer, subject);

            start = start.AddSeconds(30);

            MakeStartEvents(bert, start, timer, subject);

            //all riders have started. Martijn and bert will finsh, this will mark Richard as DNF
            start = start.AddSeconds(30);

            MakeEndEvents(martijn, start, timer, subject);

            start = start.AddSeconds(30);

            MakeEndEvents(bert, start, timer, subject);
        }

        /// <summary>
        /// Makes events for a lap begin at start
        /// </summary>
        ///<param name="rider">The rider to start the lap</param>
        ///<param name="start">Whne the lap starts</param>
        ///<param name="time">The timing unit to emit timing events</param>
        ///<param name="tracker">The tracker to receive the rider ready event</param>
        ///<param name="microseconds">microseconds from timer at start of lap</param>
        private void MakeStartEvents(Rider rider, DateTime start, MockTimingUnit time, RaceTracker tracker, long microseconds = 100)
        {
            tracker.AddEvent(new RiderReadyEventArgs(start, rider.Id, "staff"));
            time.EmitTriggerEvent(microseconds, "Timer", 0, start.AddSeconds(1));
        }

        /// <summary>
        /// Makes events for a lap finish
        /// </summary>
        ///<param name="rider">The rider to start the lap</param>
        ///<param name="end">Whne the lap ends</param>
        ///<param name="time">The timing unit to emit timing events</param>
        ///<param name="tracker">The tracker to receive the rider finished event</param>
        ///<param name="microseconds">microseconds from timer at end of lap</param>
        private void MakeEndEvents(Rider rider, DateTime end, MockTimingUnit time, RaceTracker tracker, long microseconds = 100)
        {
            List<Guid> previousUnmacthedTimes = tracker.GetState.unmatchedTimes.Select(e => e.EventId).ToList();

            // rider triggers end timing gate
            time.EmitTriggerEvent(microseconds, "Timer", 1, end);

            // wait for tracker to process events
            while (tracker.GetState.unmatchedTimes.Count == previousUnmacthedTimes.Count)
            {
                Thread.Yield();
            }

            //we need to know the id of the end timing event
            Guid timingId = tracker.GetState.unmatchedTimes.Select(e => e.EventId).Except(previousUnmacthedTimes).First();

            tracker.AddEvent(new RiderFinishedEventArgs(end.AddSeconds(1), rider.Id, "staff", timingId));
        }
    }
}
