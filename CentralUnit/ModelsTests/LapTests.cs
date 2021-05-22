using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Models;

namespace ModelsTests
{
    [TestClass]
    public class LapTests
    {
        [TestMethod]
        public void CompareTo_ShouldConsiderPernalties()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 2);
            Rider martijn = new Rider("Martijn", martijnBeacon);

            //make a fast lap with loads of penalties
            IdEvent startId = new IdEvent(DateTime.Now, martijn, "StartId", Direction.Enter);
            TimingEvent startTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            IdEvent endId = new IdEvent(DateTime.Now, martijn, "EndId", Direction.Enter);
            TimingEvent endTiming = new TimingEvent(DateTime.Now, martijn, 200, 1);

            FinishedEvent fastFinish = new FinishedEvent(startId, startTiming, endTiming, endId);

            Lap fast = new Lap(fastFinish);
            fast.AddPenalties(new List<PenaltyEvent>
            {
                new PenaltyEvent(DateTime.Now, martijn, "reason", 1, "staff"),
                new PenaltyEvent(DateTime.Now, martijn, "reason", 2, "staff")
            });

            //make a slower lap without penalties
            IdEvent slowStartId = new IdEvent(DateTime.Now, martijn, "StartId", Direction.Enter);
            TimingEvent slowStartTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            IdEvent slowEndId = new IdEvent(DateTime.Now, martijn, "EndId", Direction.Enter);
            TimingEvent slowEndTiming = new TimingEvent(DateTime.Now, martijn, 300, 1);

            FinishedEvent slowFinish = new FinishedEvent(slowStartId, slowStartTiming, slowEndTiming, slowEndId);

            Lap slow = new Lap(slowFinish);

            //without penalties the lap time should dictate the ordering
            Assert.AreEqual(1, slow.CompareTo(fast, false));
            Assert.AreEqual(-1, fast.CompareTo(slow, false));

            //with penalties the slower lap is actually faster
            Assert.AreEqual(-1, slow.CompareTo(fast, true));
            Assert.AreEqual(1, fast.CompareTo(slow, true));

            //the default should take penalties into account
            Assert.AreEqual(-1, slow.CompareTo(fast));
            Assert.AreEqual(1, fast.CompareTo(slow));
        }

        [TestMethod]
        public void CompareTo_ShouldConsiderDNFSlower()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 2);
            Rider martijn = new Rider("Martijn", martijnBeacon);

            //make a regular lap
            IdEvent startId = new IdEvent(DateTime.Now, martijn, "StartId", Direction.Enter);
            TimingEvent startTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            IdEvent endId = new IdEvent(DateTime.Now, martijn, "EndId", Direction.Enter);
            TimingEvent endTiming = new TimingEvent(DateTime.Now, martijn, 200, 1);

            FinishedEvent finish = new FinishedEvent(startId, startTiming, endTiming, endId);

            Lap normalLap = new Lap(finish);

            //make a DNF lap
            IdEvent dnfStartId = new IdEvent(DateTime.Now, martijn, "StartId", Direction.Enter);

            ManualDNFEvent manualDnf = new ManualDNFEvent(dnfStartId, "staff");
            UnitDNFEvent unitDnf = new UnitDNFEvent(finish, dnfStartId);

            Lap manualDnfLap = new Lap(manualDnf);
            Lap unitDnfLap = new Lap(unitDnf);

            //a dnf lap should always be slower (bigger) than a finished lap
            Assert.AreEqual(1, manualDnfLap.CompareTo(normalLap));
            Assert.AreEqual(-1, normalLap.CompareTo(manualDnfLap));

            Assert.AreEqual(1, unitDnfLap.CompareTo(normalLap));
            Assert.AreEqual(-1, normalLap.CompareTo(unitDnfLap));

            //dnf laps have no mutual ordering
            Assert.AreEqual(1, manualDnfLap.CompareTo(unitDnfLap));
            Assert.AreEqual(1, unitDnfLap.CompareTo(manualDnfLap));
        }

        [TestMethod]
        public void CompareTo_ShouldConsiderDSQOnFinshedLaps()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 2);
            Rider martijn = new Rider("Martijn", martijnBeacon);

            //make a fast lap 
            IdEvent startId = new IdEvent(DateTime.Now, martijn, "StartId", Direction.Enter);
            TimingEvent startTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            IdEvent endId = new IdEvent(DateTime.Now, martijn, "EndId", Direction.Enter);
            TimingEvent endTiming = new TimingEvent(DateTime.Now, martijn, 200, 1);

            FinishedEvent fastFinish = new FinishedEvent(startId, startTiming, endTiming, endId);

            Lap fast = new Lap(fastFinish);

            //make a slower lap without penalties
            IdEvent slowStartId = new IdEvent(DateTime.Now, martijn, "StartId", Direction.Enter);
            TimingEvent slowStartTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            IdEvent slowEndId = new IdEvent(DateTime.Now, martijn, "EndId", Direction.Enter);
            TimingEvent slowEndTiming = new TimingEvent(DateTime.Now, martijn, 300, 1);

            FinishedEvent slowFinish = new FinishedEvent(slowStartId, slowStartTiming, slowEndTiming, slowEndId);

            Lap slow = new Lap(slowFinish);

            //when both are not DSQ the lap time should decide the order
            Assert.AreEqual(1, slow.CompareTo(fast));
            Assert.AreEqual(-1, fast.CompareTo(slow));

            fast.SetDsq(new DSQEvent(DateTime.Now, martijn, "staff", "reason"));

            //the faster lap has a DSQ, so it should be considered slower
            Assert.AreEqual(-1, slow.CompareTo(fast));
            Assert.AreEqual(1, fast.CompareTo(slow));

            slow.SetDsq(new DSQEvent(DateTime.Now, martijn, "staff", "reason"));

            //both are now DSQ lap time should decide order again
            Assert.AreEqual(1, slow.CompareTo(fast));
            Assert.AreEqual(-1, fast.CompareTo(slow));
        }

        [TestMethod]
        public void CompareTo_ShouldConsiderDNFSlowerThanDSQ()
        {
            Beacon martijnBeacon = new Beacon(new byte[] { 0, 0, 0, 0, 0, 1 }, 2);
            Rider martijn = new Rider("Martijn", martijnBeacon);

            //make a regular lap with DSQ
            IdEvent startId = new IdEvent(DateTime.Now, martijn, "StartId", Direction.Enter);
            TimingEvent startTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            IdEvent endId = new IdEvent(DateTime.Now, martijn, "EndId", Direction.Enter);
            TimingEvent endTiming = new TimingEvent(DateTime.Now, martijn, 200, 1);

            FinishedEvent finish = new FinishedEvent(startId, startTiming, endTiming, endId);

            Lap normalLap = new Lap(finish);
            normalLap.SetDsq(new DSQEvent(DateTime.Now, martijn, "staff", "reason"));

            //make a DNF lap
            IdEvent dnfStartId = new IdEvent(DateTime.Now, martijn, "StartId", Direction.Enter);

            ManualDNFEvent manualDnf = new ManualDNFEvent(dnfStartId, "staff");
            UnitDNFEvent unitDnf = new UnitDNFEvent(finish, dnfStartId);

            Lap manualDnfLap = new Lap(manualDnf);
            Lap unitDnfLap = new Lap(unitDnf);

            //a dnf lap should always be slower (bigger) than a finished with DSQ lap
            Assert.AreEqual(1, manualDnfLap.CompareTo(normalLap));
            Assert.AreEqual(-1, normalLap.CompareTo(manualDnfLap));

            Assert.AreEqual(1, unitDnfLap.CompareTo(normalLap));
            Assert.AreEqual(-1, normalLap.CompareTo(unitDnfLap));
        }
    }
}
