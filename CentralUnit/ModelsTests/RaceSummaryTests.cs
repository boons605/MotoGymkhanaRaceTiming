using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Models.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModelsTests
{
    [TestClass]
    public class RaceSummaryTests
    {
        [TestMethod]
        public void RaceSummary_ReadAndWrite_ShouldBeSymmetric()
        {
            Rider bert = new Rider("Bert", Guid.NewGuid());
            RiderReadyEvent entered = new RiderReadyEvent(new DateTime(2000, 1, 1), bert, Guid.NewGuid(), "staff");
            TimingEvent start = new TimingEvent(new DateTime(2000, 1, 1), bert ,100, 10);
            TimingEvent end = new TimingEvent(new DateTime(2000, 1, 1), bert , 200, 11);
            RiderFinishedEvent left = new RiderFinishedEvent(new DateTime(2000, 1, 1), bert, Guid.NewGuid(), "staff", end);
            FinishedEvent finished = new FinishedEvent(entered, start, left);
            DSQEvent dsq = new DSQEvent(new DateTime(2000, 1, 1), bert, "staff", "reason");
            PenaltyEvent penalty = new PenaltyEvent(new DateTime(2000, 1, 1), bert, "reason", 3, "staff");
            ClearReadyEvent clear = new ClearReadyEvent(new DateTime(2000, 1, 1), new Rider("Martijn", Guid.NewGuid()), Guid.NewGuid(), "staff");
            DeleteTimeEvent delete = new DeleteTimeEvent(new DateTime(2000, 1, 1), Guid.NewGuid(), "staff");

            TrackerConfig config = new TrackerConfig
            {
                StartTimingGateId = 10,
                EndTimingGateId = 11
            };

            RaceSummary subject = new RaceSummary(new List<RaceEvent> {
                entered, 
                start ,
                end,
                left,
                finished,
                dsq,
                penalty,
                clear
            }, config);

            MemoryStream stream = new MemoryStream();

            subject.WriteSummary(stream);

            stream.Seek(0, SeekOrigin.Begin);

            RaceSummary parsed = RaceSummary.ReadSummary(stream);

            foreach ((Rider r1, Rider r2) in subject.Riders.Zip(parsed.Riders))
                Assert.IsTrue(CompareRiders(r1, r2));

            CollectionAssert.AreEqual(subject.Events.Select(e => e.EventId).ToList(), parsed.Events.Select(e => e.EventId).ToList());
            CollectionAssert.AreEqual(subject.Events.Select(e => e.GetType()).ToList(), parsed.Events.Select(e => e.GetType()).ToList());

            foreach ((RaceEvent e1, RaceEvent e2) in subject.Events.Zip(parsed.Events))
                Assert.IsTrue(CompareRiders(e1.Rider, e2.Rider));

            Assert.AreEqual(config.StartTimingGateId, parsed.Config.StartTimingGateId);
            Assert.AreEqual(config.EndTimingGateId, parsed.Config.EndTimingGateId);
        }

        private bool CompareRiders(Rider r1, Rider r2)
        {
            return r1.Name == r2.Name
                && r1.Id == r2.Id;
        }
    }
}
