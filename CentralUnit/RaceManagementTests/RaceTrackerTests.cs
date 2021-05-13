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
        MockRiderIdUnit startId;
        MockRiderIdUnit endId;
        MockTimingUnit timer;

        CancellationTokenSource source;

        RaceTracker subject;

        Task<RaceSummary> race;

        [TestInitialize]
        public void Init()
        {
            startId = new MockRiderIdUnit("StartId");
            endId = new MockRiderIdUnit("EndId");
            timer = new MockTimingUnit();

            source = new CancellationTokenSource();

            TrackerConfig config = new TrackerConfig
            {
                EndMatchTimeout = 10,
                StartTimingGateId = 0,
                EndTimingGateId = 1
            };

            subject = new RaceTracker(timer, startId, endId, config, new List<Rider> { });

            race = subject.Run(source.Token);
        }

        [TestMethod]
        public void OnStartId_ShouldSaveEvent()
        {
            Beacon beacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", beacon);

            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            Assert.AreEqual(1, summary.Events.Count);

            IdEvent entered = summary.Events[0] as IdEvent;
            Assert.AreEqual(entered, state.waiting[0]);

            Assert.AreEqual("Martijn", entered.Rider.Name);
            CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 1 }, entered.Rider.Beacon.Identifier);
            Assert.AreEqual(new DateTime(2000, 1, 1, 1, 1, 1), entered.Time);
        }

        [TestMethod]
        public void OnTimer_ForStart_WithoutWaitingRider_ShouldIgnoreEvent()
        {
            //0 is the start gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            Assert.AreEqual(0, summary.Events.Count);
            Assert.AreEqual(0, state.onTrack.Count);
            Assert.AreEqual(0, state.waiting.Count);
        }

        [TestMethod]
        public void OnTimer_ForStart_WitWaitingRider_ShouldMatchWithRider()
        {
            Beacon beacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", beacon);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            //we expect an EnteredEvent and a TimingEvent, in that order
            Assert.AreEqual(2, summary.Events.Count);

            IdEvent id = summary.Events[0] as IdEvent;
            TimingEvent start = summary.Events[1] as TimingEvent;
            Assert.AreEqual(state.onTrack[0].id, id);
            Assert.AreEqual(state.onTrack[0].timer, start);

            Assert.AreEqual("Martijn", start.Rider.Name);
            Assert.AreEqual(100L, start.Microseconds);
            Assert.AreEqual(new DateTime(2000, 1, 1, 1, 1, 1), start.Time);

            Assert.AreEqual(0, state.waiting.Count);
            Assert.AreEqual(0, state.unmatchedIds.Count);
            Assert.AreEqual(0, state.unmatchedTimes.Count);
        }

        [TestMethod]
        public void OnTimer_ForEnd_WithoutRiderLeft_ShouldSaveEvent()
        {
            //1 is the end gate
            timer.EmitTriggerEvent(100, "Timer", 1, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            //It is possible for the end timing gate to be triggered before the rider id is caught be the end id unit
            //save the timer event for later matching
            Assert.AreEqual(1, summary.Events.Count);

            //the state should match the summary
            TimingEvent end = summary.Events[0] as TimingEvent;
            Assert.AreEqual(state.unmatchedTimes[0], end);

            Assert.AreEqual(null, end.Rider); //a lone timer event cannot have a rider
            Assert.AreEqual(100L, end.Microseconds);
            Assert.AreEqual(new DateTime(2000, 1, 1, 1, 1, 1), end.Time);
        }

        [TestMethod]
        public void OnEndId_WithoutRiderOnTrack_ShouldIgnoreEvent()
        {
            Beacon beacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", beacon);

            endId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            Assert.AreEqual(0, summary.Events.Count);
            Assert.AreEqual(0, state.onTrack.Count);
            Assert.AreEqual(0, state.waiting.Count);
            Assert.AreEqual(0, state.unmatchedIds.Count);
            Assert.AreEqual(0, state.unmatchedTimes.Count);
        }

        [TestMethod]
        public void OnEndId_WithDifferentRiderOnTrack_ShouldIgnoreEvent()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);

            Beacon richardBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 2 }, 0);
            Rider richard = new Rider("Richard", richardBeacon);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 1));

            //rider not on track triggers end id
            endId.EmitIdEvent(richard, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            //we expect only the events for Martijn tto be recorded
            Assert.AreEqual(2, summary.Events.Count);

            IdEvent id = summary.Events[0] as IdEvent;
            TimingEvent start = summary.Events[1] as TimingEvent;
            Assert.AreEqual(state.onTrack[0].id, id);
            Assert.AreEqual(state.onTrack[0].timer, start);

            Assert.AreEqual("Martijn", start.Rider.Name);
            Assert.AreEqual(100L, start.Microseconds);
            Assert.AreEqual(new DateTime(2000, 1, 1, 1, 1, 1), start.Time);

            //no riders should be waiting at the start.
            //no end times or ids shoudl be waiting to be matched
            Assert.AreEqual(0, state.waiting.Count);
            Assert.AreEqual(0, state.unmatchedIds.Count);
            Assert.AreEqual(0, state.unmatchedTimes.Count);
        }

        [TestMethod]
        [DataRow(true, true, false)]
        [DataRow(false, false, false)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(true, true, true)]
        [DataRow(false, false, true)]
        [DataRow(true, false, true)]
        [DataRow(false, true, true)]
        public void OnEndId_WithMatchingTiming_ShouldCompleteLap(bool includeUnmatchedTime, bool includeUnmatchedId, bool flipEndEvents)
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            Beacon richardBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 2 }, 0);
            Rider richard = new Rider("Richard", richardBeacon);
            subject.AddRider(richard);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 2));

            //somebody walks through end timing gate 10 secs after rider has started
            if (includeUnmatchedTime)
            {
                timer.EmitTriggerEvent(400, "Timer", 1, new DateTime(2000, 1, 1, 1, 1, 12));
            }

            //a different rider gets too close to the stop box
            if (includeUnmatchedId)
            {
                endId.EmitIdEvent(richard, new DateTime(2000, 1, 1, 1, 1, 30));
            }

            List<Action> endEvents = new List<Action>
            { 
                //rider triggers id in stop box
                () => endId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 2, 1)),

                //rider triggers timing in stop box
                () => timer.EmitTriggerEvent(500, "Timer", 1, new DateTime(2000, 1, 1, 1, 2, 2))
            };

            if (flipEndEvents)
            {
                endEvents.Reverse();
            }

            foreach (Action a in endEvents)
            {
                a.Invoke();
            }

            source.Cancel();
            RaceSummary summary = race.Result;
            var state = subject.GetState;

            FinishedEvent finish = race.Result.Events.Last() as FinishedEvent;

            //Martijn should have done a lightning fast 400 microsecond lap
            Assert.AreEqual("Martijn", finish.Rider.Name);
            Assert.AreEqual(400L, finish.LapTime);

            //There should be nothing going on in the race at this point
            Assert.AreEqual(0, state.waiting.Count);
            Assert.AreEqual(0, state.unmatchedIds.Count);
            Assert.AreEqual(0, state.unmatchedTimes.Count);
            Assert.AreEqual(0, state.onTrack.Count);
        }

        [TestMethod]
        [DataRow(10)]
        [DataRow(-10)]
        public void OnEndId_ShouldRespectTimeout(int timeDifference)
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 20));

            //rider triggers timing in stop box
            timer.EmitTriggerEvent(500, "Timer", 1, new DateTime(2000, 1, 1, 1, 2, 20));

            //end id is triggered 11 seconds apart, should not match
            endId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 2, 20 + timeDifference + Math.Sign(timeDifference)));

            //end id is triggered 10 seconds apart, should match
            endId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 2, 20 + timeDifference));

            source.Cancel();
            RaceSummary summary = race.Result;
            var state = subject.GetState;

            FinishedEvent finish = summary.Events.Last() as FinishedEvent;

            Assert.AreEqual(20 + timeDifference, finish.Left.Time.Second);

            //There should be nothing going on in the race at this point
            Assert.AreEqual(0, state.waiting.Count);
            Assert.AreEqual(0, state.unmatchedTimes.Count);
            Assert.AreEqual(0, state.onTrack.Count);

            //depending on whether the timeDifference is negative or positive the unmatched time should be cleared
            //on positive timeDifference the unmatched time is not old enough to be cleared
            if (timeDifference > 0)
            {
                Assert.AreEqual(1, state.unmatchedIds.Count);
            }
            else
            {
                Assert.AreEqual(0, state.unmatchedIds.Count);
            }
        }

        [TestMethod]
        [DataRow(10)]
        [DataRow(-10)]
        public void OnTimer_ForEnd_ShouldRespectTimeout(int timeDifference)
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 20));

            //rider triggers id in stop box
            endId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 2, 20));

            //end timer triggered 11 seconds apart, should not match
            timer.EmitTriggerEvent(500, "Timer", 1, new DateTime(2000, 1, 1, 1, 2, 20 + timeDifference + Math.Sign(timeDifference)));

            //end timer triggered 10 seconds apart, should match
            timer.EmitTriggerEvent(500, "Timer", 1, new DateTime(2000, 1, 1, 1, 2, 20 + timeDifference));

            source.Cancel();
            RaceSummary summary = race.Result;
            var state = subject.GetState;

            FinishedEvent finish = summary.Events.Last() as FinishedEvent;

            Assert.AreEqual(20 + timeDifference, finish.TimeEnd.Time.Second);

            //There should be nothing going on in the race at this point
            Assert.AreEqual(0, state.waiting.Count);
            Assert.AreEqual(0, state.unmatchedIds.Count);
            Assert.AreEqual(0, state.onTrack.Count);

            //depending on whether the timeDifference is negative or positive the unmatched id should be cleared
            //on positive timeDifference the unmatched time is not old enough to be cleared
            if (timeDifference > 0)
            {
                Assert.AreEqual(1, state.unmatchedTimes.Count);
            }
            else
            {
                Assert.AreEqual(0, state.unmatchedTimes.Count);
            }
        }

        [TestMethod]
        public void RaceWithMultipleOnTrack_AndDNF_ShouldWork()
        {
            SimulateRaceWithDNF();

            source.Cancel();
            RaceSummary summary = race.Result;
            var state = subject.GetState;

            List<FinishedEvent> finishes = summary.Events.Where(e => e is FinishedEvent).Select(e => e as FinishedEvent).ToList();
            UnitDNFEvent dnf = summary.Events.Last() as UnitDNFEvent;

            Assert.AreEqual("Martijn", finishes[0].Rider.Name);
            Assert.AreEqual("Bert", finishes[1].Rider.Name);

            Assert.AreEqual("Richard", dnf.Rider.Name);
            Assert.AreEqual("Bert", dnf.OtherRider.Rider.Name);

            //There should be nothing going on in the race at this point
            Assert.AreEqual(0, state.waiting.Count);
            Assert.AreEqual(0, state.unmatchedIds.Count);
            Assert.AreEqual(0, state.onTrack.Count);
            Assert.AreEqual(0, state.unmatchedTimes.Count);
        }

        [TestMethod]
        public void OnEndId_WithAccidentalEndId_ShouldMatch()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 1));

            //rider triggers id in stop box accidentally (maybe the track was constructed to pass too close to stop box
            endId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 2, 20));

            //rider triggers id in stop box for real five seconds later
            endId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 2, 25));

            //end timer triggered 1 second later
            timer.EmitTriggerEvent(500, "Timer", 1, new DateTime(2000, 1, 1, 1, 2, 26));

            source.Cancel();
            RaceSummary summary = race.Result;
            var state = subject.GetState;

            FinishedEvent finish = summary.Events.Last() as FinishedEvent;
            Assert.AreEqual(25, finish.Left.Time.Second);

            //There should be nothing going on in the race at this point, except for the lingering end id event
            Assert.AreEqual(0, state.waiting.Count);
            Assert.AreEqual(0, state.onTrack.Count);
            Assert.AreEqual(0, state.unmatchedTimes.Count);

            Assert.AreEqual(1, state.unmatchedIds.Count);
        }

        [TestMethod]
        public void OnRiderWaiting_WithFirstAndSecondRider_ShouldFire()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            Beacon bertBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 2 }, 0);
            Rider bert = new Rider("Bert", bertBeacon);
            subject.AddRider(bert);

            string waiting = null;

            subject.OnRiderWaiting += (obj, args) => waiting = args.Rider.Rider.Name;

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //the race tracker is processing events in a different thread, we must wait for it
            Thread.Sleep(1000);

            //Martijn should be flagged as ready to start
            Assert.AreEqual(waiting, "Martijn");

            //Second rider enters the queue to start
            startId.EmitIdEvent(bert, new DateTime(2000, 1, 1, 1, 2, 1));

            //the race tracker is processing events in a different thread, we must wait for it
            Thread.Sleep(1000);

            //OnRiderWaiting should not be fired since Martijn has not left
            Assert.AreEqual(waiting, "Martijn");

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            source.Cancel();
            race.Wait();

            //Bert moves to the front of the waiting queue, so he is now ready to start
            Assert.AreEqual(waiting, "Bert");
        }

        [TestMethod]
        public void OnStartEmpty_WhenLastRiderStart_ShouldFire()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            Beacon bertBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 2 }, 0);
            Rider bert = new Rider("Bert", bertBeacon);
            subject.AddRider(bert);

            bool isEmpty = false;

            subject.OnStartEmpty += (obj, args) => isEmpty = true;

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //the race tracker is processing events in a different thread, we must wait for it
            Thread.Sleep(1000);

            //event should not have fired
            Assert.IsFalse(isEmpty);

            //Second rider enters the queue to start
            startId.EmitIdEvent(bert, new DateTime(2000, 1, 1, 1, 2, 1));

            //event should not have fired
            Assert.IsFalse(isEmpty);

            //Martijn and Bert trigger timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 2));

            source.Cancel();
            race.Wait();

            Assert.IsTrue(isEmpty);
        }

        [TestMethod]
        public void OnRiderFinished_WithFinishAndDNF_ShouldFireForFinish()
        {
            List<string> finished = new List<string>();

            subject.OnRiderFinished += (obj, args) => finished.Add(args.Lap.Rider.Name);

            SimulateRaceWithDNF();

            source.Cancel();
            race.Wait();

            Assert.AreEqual(2, finished.Count);
            Assert.AreEqual("Martijn", finished[0]);
            Assert.AreEqual("Bert", finished[1]);

            Assert.AreEqual(3, startId.KnownRiders.Count);
            Assert.AreEqual("Martijn", startId.KnownRiders[0].Name);
            Assert.AreEqual("Bert", startId.KnownRiders[1].Name);
            Assert.AreEqual("Richard", startId.KnownRiders[2].Name);
        }

        [TestMethod]
        public void OnlyWaitingRider_LeavingRangeOfStart_ShouldRemoveThem()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            bool isEmpty = false;

            subject.OnStartEmpty += (obj, args) => isEmpty = true;

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //the race tracker is processing events in a different thread, we must wait for it
            Thread.Sleep(1000);

            //event should not have fired
            Assert.IsFalse(isEmpty);

            startId.EmitExitEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();
            race.Wait();

            var state = subject.GetState;
            Assert.AreEqual(0, state.waiting.Count);

            Assert.IsTrue(isEmpty);
        }

        [TestMethod]
        public void FirstWaitingRider_LeavingRangeOfStart_ShouldMoveSecondUp()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            Beacon bertBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 2 }, 0);
            Rider bert = new Rider("Bert", bertBeacon);
            subject.AddRider(bert);

            bool isEmpty = false;

            subject.OnStartEmpty += (obj, args) => isEmpty = true;

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));
            startId.EmitIdEvent(bert, new DateTime(2000, 1, 1, 1, 1, 1));

            //the race tracker is processing events in a different thread, we must wait for it
            Thread.Sleep(1000);

            //event should not have fired
            Assert.IsFalse(isEmpty);

            //Martijn and Bert trigger timing gate
            startId.EmitExitEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();
            race.Wait();

            var state = subject.GetState;
            Assert.AreEqual(1, state.waiting.Count);
            Assert.AreEqual(bert, state.waiting[0].Rider);

            Assert.IsFalse(isEmpty);
        }

        [TestMethod]
        public void RiderOnTrack_LeavingRangeOfStart_ShouldRemoveThemFromUnit()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));
            startId.EmitExitEvent(martijn, new DateTime(2000, 1, 1, 1, 2, 1));

            source.Cancel();
            race.Wait();

            var state = subject.GetState;

            Assert.AreEqual(1, state.onTrack.Count);
            Assert.AreEqual(martijn, state.onTrack[0].id.Rider);

            Assert.AreEqual(0, startId.KnownRiders.Count);
        }

        [TestMethod]
        public void RiderEnteringTrack_ShouldAddToEndUnit()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            source.Cancel();
            race.Wait();

            Assert.AreEqual(1, endId.KnownRiders.Count);
        }

        [TestMethod]
        public void ManualDnf_WithRiderOnTrack_ShouldEndLap()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            subject.AddEvent(new ManualDNFEventArgs(DateTime.Now, martijn.Name, "staff"));

            source.Cancel();
            race.Wait();

            Assert.AreEqual(0, endId.KnownRiders.Count);
            Assert.AreEqual(1, subject.Laps.Count);

            Lap lap = subject.Laps[0];

            Assert.IsTrue(lap.End is ManualDNFEvent);
            Assert.AreEqual(-1, lap.LapTime);
            Assert.IsFalse(lap.Disqualified);
        }

        [TestMethod]
        public void ManualDnf_WithoutRiderOnTrack_ShouldBeIgnored()
        {
            subject.AddEvent(new ManualDNFEventArgs(DateTime.Now, "Nope", "staff"));

            source.Cancel();
            race.Wait();

            Assert.AreEqual(0, endId.KnownRiders.Count);
            Assert.AreEqual(0, subject.Laps.Count);
        }

        [TestMethod]
        public void ManualDnf_WithDifferentRiderOnTrack_ShouldBeIgnored()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            subject.AddEvent(new ManualDNFEventArgs(DateTime.Now, "Nope", "staff"));

            source.Cancel();
            race.Wait();

            source.Cancel();
            race.Wait();

            Assert.AreEqual(1, endId.KnownRiders.Count);
            Assert.AreEqual(0, subject.Laps.Count);
        }

        [TestMethod]
        public void ManualDnf_WithMultipleRidersOnTrack_ShouldPickCorrect()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            Beacon bertBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 2 }, 0);
            Rider bert = new Rider("Bert", martijnBeacon);
            subject.AddRider(bert);

            //rider enters start box
            startId.EmitIdEvent(martijn, new DateTime(2000, 1, 1, 1, 1, 1));

            //Martijn triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 1));

            startId.EmitIdEvent(bert, new DateTime(2000, 1, 1, 1, 1, 2));
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 2, 2));

            subject.AddEvent(new ManualDNFEventArgs(DateTime.Now, "Bert", "staff"));

            source.Cancel();
            race.Wait();

            source.Cancel();
            race.Wait();

            Assert.AreEqual(1, endId.KnownRiders.Count);
            Assert.AreEqual(martijn, endId.KnownRiders[0]);
            Assert.AreEqual(1, subject.Laps.Count);

            Lap lap = subject.Laps[0];

            Assert.IsTrue(lap.End is ManualDNFEvent);
            Assert.AreEqual(-1, lap.LapTime);
            Assert.IsFalse(lap.Disqualified);
            Assert.AreEqual(bert, lap.Rider);
        }

        /// <summary>
        /// Simulates a race where Martijn, Richard and Bert start, but only Martijn and Bert finish
        /// </summary>
        private void SimulateRaceWithDNF()
        {
            DateTime start = new DateTime(2000, 1, 1, 1, 1, 1);

            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 0);
            Rider martijn = new Rider("Martijn", martijnBeacon);
            subject.AddRider(martijn);

            Beacon richardBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 2 }, 0);
            Rider richard = new Rider("Richard", richardBeacon);
            subject.AddRider(richard);

            Beacon bertBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 3 }, 0);
            Rider bert = new Rider("Bert", bertBeacon);
            subject.AddRider(bert);

            MakeStartEvents(martijn, start, startId, timer);

            start = start.AddSeconds(30);

            MakeStartEvents(richard, start, startId, timer);

            start = start.AddSeconds(30);

            MakeStartEvents(bert, start, startId, timer);

            //all riders have started. Martijn and bert will finsh, this will mark Richard as DNF
            start = start.AddSeconds(30);

            MakeEndEvents(martijn, start, endId, timer);

            start = start.AddSeconds(30);

            MakeEndEvents(bert, start, endId, timer);
        }

        /// <summary>
        /// Makes events for a lap begin at start
        /// </summary>
        /// <param name="riderName"></param>
        /// <param name="sensorId"></param>
        /// <param name="start"></param>
        /// <param name="id"></param>
        /// <param name="time"></param>
        private void MakeStartEvents(Rider rider, DateTime start, MockRiderIdUnit id, MockTimingUnit time)
        {
            id.EmitIdEvent(rider, start);
            time.EmitTriggerEvent(100, "Timer", 0, start);
            id.EmitExitEvent(rider, start);
        }

        /// <summary>
        /// Makes events for a lap finish at start + 1 minute
        /// </summary>
        /// <param name="riderName"></param>
        /// <param name="sensorId"></param>
        /// <param name="end"></param>
        /// <param name="id"></param>
        /// <param name="time"></param>
        private void MakeEndEvents(Rider rider, DateTime end, MockRiderIdUnit id, MockTimingUnit time)
        {
            id.EmitIdEvent(rider, end);
            time.EmitTriggerEvent(100, "Timer", 1, end);
            id.EmitExitEvent(rider, end);
        }
    }
}
