using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaceManagement;
using RaceManagementTests.TestHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RaceManagementTests
{
    [TestClass]
    public class RaceTrackerTests
    {
        [TestMethod]
        public void OnStartId_ShouldSaveEvent()
        {
            MockRiderIdUnit startId = new MockRiderIdUnit();
            MockRiderIdUnit endId = new MockRiderIdUnit();
            MockTimingUnit timer = new MockTimingUnit();

            CancellationTokenSource source = new CancellationTokenSource();

            RaceTracker subject = new RaceTracker(timer, startId, endId, 0, 1);

            Task<RaceSummary> race = subject.Run(source.Token);

            startId.EmitIdEvent("Martijn", new byte[] { 0, 1 }, new DateTime(2000, 1, 1, 1, 1, 1), "StartId");

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            Assert.AreEqual(1, summary.Events.Count);

            EnteredEvent entered = summary.Events[0] as EnteredEvent;
            Assert.AreEqual(entered, state.waiting[0]);

            Assert.AreEqual("Martijn", entered.Rider);
            CollectionAssert.AreEqual(new byte[] { 0, 1 }, entered.SensorId);
            Assert.AreEqual(new DateTime(2000, 1, 1, 1, 1, 1), entered.Time);
        }

        [TestMethod]
        public void OnTimer_ForStart_WithoutWaitingRider_ShouldIgnoreEvent()
        {
            MockRiderIdUnit startId = new MockRiderIdUnit();
            MockRiderIdUnit endId = new MockRiderIdUnit();
            MockTimingUnit timer = new MockTimingUnit();

            CancellationTokenSource source = new CancellationTokenSource();

            RaceTracker subject = new RaceTracker(timer, startId, endId, 0, 1);

            Task<RaceSummary> race = subject.Run(source.Token);

            //0 is the start gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000,1,1,1,1,1));

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
            MockRiderIdUnit startId = new MockRiderIdUnit();
            MockRiderIdUnit endId = new MockRiderIdUnit();
            MockTimingUnit timer = new MockTimingUnit();

            CancellationTokenSource source = new CancellationTokenSource();

            RaceTracker subject = new RaceTracker(timer, startId, endId, 0, 1);

            Task<RaceSummary> race = subject.Run(source.Token);

            //rider enters start box
            startId.EmitIdEvent("Martijn", new byte[] { 0, 1 }, new DateTime(2000, 1, 1, 1, 1, 1), "StartId");
            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 1));

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            //we expect an EnteredEvent and a TimingEvent, in that order
            Assert.AreEqual(2, summary.Events.Count);

            EnteredEvent id = summary.Events[0] as EnteredEvent;
            TimingEvent start = summary.Events[1] as TimingEvent;
            Assert.AreEqual(state.onTrack[0].id, id);
            Assert.AreEqual(state.onTrack[0].timer, start);

            Assert.AreEqual("Martijn", start.Rider);
            Assert.AreEqual(100l, start.Microseconds);
            Assert.AreEqual(new DateTime(2000, 1, 1, 1, 1, 1), start.Time);

            Assert.AreEqual(0, state.waiting.Count);
            Assert.AreEqual(0, state.unmatchedIds.Count);
            Assert.AreEqual(0, state.unmatchedTimes.Count);
        }

        [TestMethod]
        public void OnTimer_ForEnd_WithoutRiderLeft_ShouldSaveEvent()
        {
            MockRiderIdUnit startId = new MockRiderIdUnit();
            MockRiderIdUnit endId = new MockRiderIdUnit();
            MockTimingUnit timer = new MockTimingUnit();

            CancellationTokenSource source = new CancellationTokenSource();

            RaceTracker subject = new RaceTracker(timer, startId, endId, 0, 1);

            Task<RaceSummary> race = subject.Run(source.Token);

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

            Assert.AreEqual(null, end.Rider);//a lone timer event cannot have a rider
            Assert.AreEqual(100L, end.Microseconds);
            Assert.AreEqual(new DateTime(2000, 1, 1, 1, 1, 1), end.Time);
        }

        [TestMethod]
        public void OnEndId_WithoutRiderOnTrack_ShouldIgnoreEvent()
        {
            MockRiderIdUnit startId = new MockRiderIdUnit();
            MockRiderIdUnit endId = new MockRiderIdUnit();
            MockTimingUnit timer = new MockTimingUnit();

            CancellationTokenSource source = new CancellationTokenSource();

            RaceTracker subject = new RaceTracker(timer, startId, endId, 0, 1);

            Task<RaceSummary> race = subject.Run(source.Token);

            endId.EmitIdEvent("Martijn", new byte[] { 0, 1 }, new DateTime(2000, 1, 1, 1, 1, 1), "StartId");

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
            MockRiderIdUnit startId = new MockRiderIdUnit();
            MockRiderIdUnit endId = new MockRiderIdUnit();
            MockTimingUnit timer = new MockTimingUnit();

            CancellationTokenSource source = new CancellationTokenSource();

            RaceTracker subject = new RaceTracker(timer, startId, endId, 0, 1);

            Task<RaceSummary> race = subject.Run(source.Token);

            //rider enters start box
            startId.EmitIdEvent("Martijn", new byte[] { 0, 1 }, new DateTime(2000, 1, 1, 1, 1, 1), "StartId");
            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 1));

            //rider not on track triggers end id
            endId.EmitIdEvent("Richard", new byte[] { 0, 2 }, new DateTime(2000, 1, 1, 1, 1, 1), "StartId");

            source.Cancel();

            RaceSummary summary = race.Result;
            var state = subject.GetState;

            //we expect only the events for Martijn tto be recorded
            Assert.AreEqual(2, summary.Events.Count);

            EnteredEvent id = summary.Events[0] as EnteredEvent;
            TimingEvent start = summary.Events[1] as TimingEvent;
            Assert.AreEqual(state.onTrack[0].id, id);
            Assert.AreEqual(state.onTrack[0].timer, start);

            Assert.AreEqual("Martijn", start.Rider);
            Assert.AreEqual(100l, start.Microseconds);
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
            MockRiderIdUnit startId = new MockRiderIdUnit();
            MockRiderIdUnit endId = new MockRiderIdUnit();
            MockTimingUnit timer = new MockTimingUnit();

            CancellationTokenSource source = new CancellationTokenSource();

            RaceTracker subject = new RaceTracker(timer, startId, endId, 0, 1);

            Task<RaceSummary> race = subject.Run(source.Token);

            //rider enters start box
            startId.EmitIdEvent("Martijn", new byte[] { 0, 1 }, new DateTime(2000, 1, 1, 1, 1, 1), "StartId");
            //rider triggers timing gate
            timer.EmitTriggerEvent(100, "Timer", 0, new DateTime(2000, 1, 1, 1, 1, 2));

            //somebody walks through end timing gate 10 secs after rider has started
            if (includeUnmatchedTime)
                timer.EmitTriggerEvent(400, "Timer", 1, new DateTime(2000, 1, 1, 1, 1, 12));

            //a different rider gets too close to the stop box
            if (includeUnmatchedId)
                endId.EmitIdEvent("Richard", new byte[] { 0, 2 }, new DateTime(2000, 1, 1, 1, 1, 30), "EndId");

            List<Action> endEvents = new List<Action>
            { 
                //rider triggers id in stop box
                () => endId.EmitIdEvent("Martijn", new byte[] { 0, 1 }, new DateTime(2000, 1, 1, 1, 2, 1), "EndId"),
                //rider triggers timing in stop box
                () => timer.EmitTriggerEvent(500, "Timer", 1, new DateTime(2000, 1, 1, 1, 2, 2))
            };

            if (flipEndEvents)
                endEvents.Reverse();
            foreach (Action a in endEvents)
                a.Invoke();

            source.Cancel();
            RaceSummary summary = race.Result;
            var state = subject.GetState;

            FinishedEvent finish = race.Result.Events.Last() as FinishedEvent;

            //Martijn should have done a lightning fast 400 microsecond lap
            Assert.AreEqual("Martijn", finish.Rider);
            Assert.AreEqual(400L, finish.LapTime);

            //There should be nothing going on in the race at this point
            Assert.AreEqual(0, state.waiting.Count);
            Assert.AreEqual(0, state.unmatchedIds.Count);
            Assert.AreEqual(0, state.unmatchedTimes.Count);
            Assert.AreEqual(0, state.onTrack.Count);
        }
    }
}
