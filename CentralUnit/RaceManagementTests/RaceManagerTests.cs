using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DisplayUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Models.Config;
using RaceManagement;
using RaceManagementTests.TestHelpers;

namespace RaceManagementTests
{
    [TestClass]
    public class RaceManagerTests
    {
        MockTimingUnit timer;

        RaceTracker tracker;
        RaceManager subject;

        [TestInitialize]
        public void Init()
        {
            timer = new MockTimingUnit();

            TrackerConfig config = new TrackerConfig
            {
                EndMatchTimeout = 20,
                StartTimingGateId = 0,
                EndTimingGateId = 1
            };

            //riders are addded in the SimulateRace method
            tracker = new RaceTracker(timer, config, new List<Rider>());

            subject = new RaceManager();
            subject.Start(tracker, new List<IDisplayUnit> { timer });

            SimulateRace();

            //wait for the tracker to process all events
            Thread.Sleep(2000);

            subject.Stop();
        }

        [TestMethod]
        public void GetLapTimes_WithoutArgument_ShouldReturnAllLaps()
        {
            List<Lap> laps = subject.GetLapTimes();

            //first set of laps
            //DNFs ar also counted as a kind of lap
            Assert.AreEqual(8, laps.Count);

            Assert.AreEqual("Rider-1", laps[0].Rider.Name);
            Assert.AreEqual(10000000, laps[0].GetLapTime());

            //A DNF
            Assert.AreEqual("Rider-0", laps[1].Rider.Name);
            Assert.AreEqual(-1, laps[1].GetLapTime());

            Assert.AreEqual("Rider-2", laps[2].Rider.Name);
            Assert.AreEqual(20000000, laps[2].GetLapTime());

            Assert.AreEqual("Rider-3", laps[3].Rider.Name);
            Assert.AreEqual(30000000, laps[3].GetLapTime());

            //second set of laps
            Assert.AreEqual("Rider-0", laps[4].Rider.Name);
            Assert.AreEqual(50000000, laps[4].GetLapTime());

            Assert.AreEqual("Rider-2", laps[5].Rider.Name);
            Assert.AreEqual(40000000, laps[5].GetLapTime());

            Assert.AreEqual("Rider-1", laps[6].Rider.Name);
            Assert.AreEqual(-1, laps[6].GetLapTime());

            Assert.AreEqual("Rider-3", laps[7].Rider.Name);
            Assert.AreEqual(15000000, laps[7].GetLapTime());
        }

        [TestMethod]
        public void GetLapTimes_WithArgument_ShouldReturnCorrectLaps()
        {
            List<Lap> laps = subject.GetLapTimes();

            for(int i = 0; i<8; i++)
            {
                List<Lap> partialLaps = subject.GetLapTimes(i);
                CollectionAssert.AreEqual(laps.Skip(i).ToList(), partialLaps);
            }
        }

        [TestMethod]
        public void GetLapTimes_WithoutLaps_ShouldReturnEmptyList()
        {
            MockRaceTracker tracker = new MockRaceTracker();
            RaceManager manager = new RaceManager();
            manager.Start(tracker, new List<IDisplayUnit> { });

            List<Lap>laps = manager.GetLapTimes();

            Assert.AreEqual(0, laps.Count);
        }

        [TestMethod]
        public void GetBestLaps_ShouldReturn_SortedShortestTimes()
        {
            List<Lap> laps = subject.GetBestLaps();

            //4 riders means 4 best laps
            Assert.AreEqual(4, laps.Count);

            Assert.AreEqual("Rider-1", laps[0].Rider.Name);
            Assert.AreEqual(10000000, laps[0].GetLapTime());

            Assert.AreEqual("Rider-3", laps[1].Rider.Name);
            Assert.AreEqual(15000000, laps[1].GetLapTime());

            Assert.AreEqual("Rider-2", laps[2].Rider.Name);
            Assert.AreEqual(20000000, laps[2].GetLapTime());

            Assert.AreEqual("Rider-0", laps[3].Rider.Name);
            Assert.AreEqual(50000000, laps[3].GetLapTime());
        }

        [TestMethod]
        public void GetBestLaps_WithoutLaps_ShouldReturnEmptyList()
        {
            MockRaceTracker tracker = new MockRaceTracker();
            RaceManager manager = new RaceManager();
            manager.Start(tracker, new List<IDisplayUnit> { });

            List<Lap> laps = manager.GetBestLaps();

            Assert.AreEqual(0, laps.Count);
        }

        /// <summary>
        /// Simulates a race that features 4 riders
        /// Rider-0: lap1 dnf, lap2 finish
        /// Rider-1: lap1 finish, lap2 dnf
        /// Rider-2: lap1 fast, lap2 slow
        /// Rider-3: lap1 slow, lap2 fast
        /// </summary>
        private void SimulateRace()
        {
            List<Rider> riders = new List<Rider>();
            for (byte i = 0; i < 4; i++)
            {
                Rider rider = new Rider($"Rider-{i}", Guid.NewGuid());
                riders.Add(rider);
                tracker.AddRider(rider);
            }

            DateTime currentTime = new DateTime(2000, 1, 1);

            //first laps for all riders
            MakeDNF(riders[0], currentTime, tracker, timer);
            currentTime = currentTime.AddSeconds(1);

            currentTime = MakeLap(riders[1], currentTime, 10000000, tracker, timer);

            currentTime = MakeLap(riders[2], currentTime, 20000000, tracker, timer);

            currentTime = MakeLap(riders[3], currentTime, 30000000, tracker, timer);

            //second laps for all riders
            currentTime = MakeLap(riders[0], currentTime, 50000000, tracker, timer);

            MakeDNF(riders[1], currentTime, tracker, timer);
            currentTime = currentTime.AddSeconds(1);

            currentTime = MakeLap(riders[2], currentTime, 40000000, tracker, timer);

            MakeLap(riders[3], currentTime, 15000000, tracker, timer);
        }

        /// <summary>
        /// Makes the provided units emit events consistent with the rider completing a lap
        /// </summary>
        /// <param name="rider"></param>
        /// <param name="startTime"></param>
        /// <param name="lapMicroseconds"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="time"></param>
        /// <returns> the time at which the rider finishes the lap (start + lap time)</returns>
        private DateTime MakeLap(Rider rider, DateTime startTime, long lapMicroseconds, RaceTracker tracker, MockTimingUnit time)
        {
            long startMicroseconds = startTime.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
            long endMicroseconds = startMicroseconds + lapMicroseconds;

            DateTime endTime = startTime.AddMilliseconds(lapMicroseconds / 1000);

            MakeStartEvents(rider, startTime, startMicroseconds, tracker, time);
            MakeEndEvents(rider, endTime, endMicroseconds, tracker, time);

            return endTime;
        }

        private void MakeDNF(Rider rider, DateTime startTime, RaceTracker tracker, MockTimingUnit time)
        {
            long startMicroseconds = startTime.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
            MakeStartEvents(rider, startTime, startMicroseconds, tracker, time);

            tracker.AddEvent(new ManualDNFEventArgs(startTime, rider.Id, "staff"));
        }

        /// <summary>
        /// Makes events for a lap begin at start
        /// </summary>
        /// <param name="rider">The rider that is starting</param>
        /// <param name="start">The moment when the events are received</param>
        /// <param name="microseconds">The value of the timer at the timer event</param>
        /// <param name="tracker">The tracker to emit the events to</param>
        /// <param name="time">The timing unit that should emit the timing events</param>
        private void MakeStartEvents(Rider rider, DateTime start, long microseconds, RaceTracker tracker, MockTimingUnit time)
        {
            tracker.AddEvent(new RiderReadyEventArgs(start, rider.Id, "staff"));
            time.EmitTriggerEvent(microseconds, "Timer", 0, start);
        }

        /// <summary>
        /// Makes events for a lap finish at start + 1 minute
        /// </summary>
        /// <param name="rider">The rider that is finishing</param>
        /// <param name="start">The moment when the events are received</param>
        /// <param name="microseconds">The value of the timer at the timer event</param>
        /// <param name="tracker">The tracker to emit the events to</param>
        /// <param name="time">The timing unit that should emit the timing events</param>
        private void MakeEndEvents(Rider rider, DateTime end, long microseconds, RaceTracker tracker, MockTimingUnit time)
        {
            tracker.AddEvent(new RiderFinishedEventArgs(end, rider.Id, "staff", Guid.NewGuid()));
            time.EmitTriggerEvent(microseconds, "Timer", 1, end);
        }
    }
}
