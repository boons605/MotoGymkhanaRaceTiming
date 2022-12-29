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
            Guid martijnId = Guid.NewGuid();
            Guid bertId = Guid.NewGuid();

            RiderReadyEvent entered = new RiderReadyEvent(new DateTime(2000, 1, 1), new Rider("Martijn", martijnId), Guid.NewGuid(), "staff");
            TimingEvent timing = new TimingEvent(new DateTime(2000, 1, 1), new Rider("Bert", bertId),100, 1);

            TrackerConfig config = new TrackerConfig
            {
                EndMatchTimeout = 9,
                StartTimingGateId = 10,
                EndTimingGateId = 11
            };

            RaceSummary subject = new RaceSummary(new List<RaceEvent> { entered, timing }, config);

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
            Assert.AreEqual(config.EndMatchTimeout, parsed.Config.EndMatchTimeout);
        }

        private bool CompareRiders(Rider r1, Rider r2)
        {
            return r1.Name == r2.Name
                && r1.Id == r2.Id;
        }
    }
}
