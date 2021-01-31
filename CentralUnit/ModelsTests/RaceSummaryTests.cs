using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
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
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 2);
            martijnBeacon.Rssi = 3;
            martijnBeacon.MeasuredPower = 4;

            Beacon bertBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 5 }, 6);
            bertBeacon.Rssi = 7;
            bertBeacon.MeasuredPower = 8;

            EnteredEvent entered = new EnteredEvent(new DateTime(2000, 1, 1), new Rider("Martijn", martijnBeacon));
            TimingEvent timing = new TimingEvent(new DateTime(2000, 1, 1), new Rider("Bert", bertBeacon),100, 1);

            RaceSummary subject = new RaceSummary(new List<RaceEvent> { entered, timing });

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
        }

        private bool CompareRiders(Rider r1, Rider r2)
        {
            return r1.Name == r2.Name
                && r1.Beacon.Identifier.Zip(r2.Beacon.Identifier).All(pair => pair.First == pair.Second)//verify all identifier bytes are equal
                && r1.Beacon.CorrectionFactor == r2.Beacon.CorrectionFactor
                && r1.Beacon.Distance == r2.Beacon.Distance
                && r1.Beacon.MeasuredPower == r2.Beacon.MeasuredPower
                && r1.Beacon.Rssi == r2.Beacon.Rssi;
        }
    }
}
