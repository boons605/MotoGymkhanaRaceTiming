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
            Rider martijn = new Rider("Martijn", Guid.NewGuid());

            //make a fast lap with loads of penalties
            RiderReadyEvent fastReady = new RiderReadyEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff");
            TimingEvent fastStartTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            TimingEvent fastEndTiming = new TimingEvent(DateTime.Now, martijn, 200, 1);
            RiderFinishedEvent fastEnd = new RiderFinishedEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff", fastEndTiming);

            FinishedEvent fastFinish = new FinishedEvent(fastReady, fastStartTiming, fastEnd);

            Lap fast = new Lap(fastFinish);
            fast.AddPenalties(new List<PenaltyEvent>
            {
                new PenaltyEvent(DateTime.Now, martijn, "reason", 1, "staff"),
                new PenaltyEvent(DateTime.Now, martijn, "reason", 2, "staff")
            });

            //make a slower lap without penalties
            RiderReadyEvent slowReady = new RiderReadyEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff");
            TimingEvent slowStartTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            TimingEvent slowEndTiming = new TimingEvent(DateTime.Now, martijn, 300, 1);
            RiderFinishedEvent slowEnd = new RiderFinishedEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff", slowEndTiming);

            FinishedEvent slowFinish = new FinishedEvent(slowReady, slowStartTiming, slowEnd);

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
            Rider martijn = new Rider("Martijn", Guid.NewGuid());

            //make a regular lap
            RiderReadyEvent startId = new RiderReadyEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff");
            TimingEvent startTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);

            TimingEvent endTiming = new TimingEvent(DateTime.Now, martijn, 200, 1);
            RiderFinishedEvent endId = new RiderFinishedEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff", endTiming);

            FinishedEvent finish = new FinishedEvent(startId, startTiming, endId);

            Lap normalLap = new Lap(finish);

            //make a DNF lap
            RiderReadyEvent dnfStartId = new RiderReadyEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff");

            ManualDNFEvent manualDnf = new ManualDNFEvent(dnfStartId, "staff");

            Lap manualDnfLap = new Lap(manualDnf);

            //a dnf lap should always be slower (bigger) than a finished lap
            Assert.AreEqual(1, manualDnfLap.CompareTo(normalLap));
            Assert.AreEqual(-1, normalLap.CompareTo(manualDnfLap));
        }

        [TestMethod]
        public void CompareTo_ShouldConsiderDSQOnFinshedLaps()
        {
            Rider martijn = new Rider("Martijn", Guid.NewGuid());

            //make a fast lap 
            RiderReadyEvent fastReady = new RiderReadyEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff");
            TimingEvent fastStartTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            TimingEvent fastEndTiming = new TimingEvent(DateTime.Now, martijn, 200, 1);
            RiderFinishedEvent fastEnd = new RiderFinishedEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff", fastEndTiming);

            FinishedEvent fastFinish = new FinishedEvent(fastReady, fastStartTiming, fastEnd);

            Lap fast = new Lap(fastFinish);

            //make a slower lap without penalties
            RiderReadyEvent slowStartId = new RiderReadyEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff");
            TimingEvent slowStartTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            TimingEvent slowEndTiming = new TimingEvent(DateTime.Now, martijn, 300, 1);
            RiderFinishedEvent slowEnd = new RiderFinishedEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff", slowEndTiming);

            FinishedEvent slowFinish = new FinishedEvent(slowStartId, slowStartTiming, slowEnd);

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
            Rider martijn = new Rider("Martijn", Guid.NewGuid());

            //make a regular lap with DSQ
            RiderReadyEvent ready = new RiderReadyEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff");
            TimingEvent startTiming = new TimingEvent(DateTime.Now, martijn, 100, 0);
            TimingEvent endTiming = new TimingEvent(DateTime.Now, martijn, 200, 1);
            RiderFinishedEvent end = new RiderFinishedEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff", endTiming);

            FinishedEvent finish = new FinishedEvent(ready, startTiming, end);

            Lap dsqLap = new Lap(finish);
            dsqLap.SetDsq(new DSQEvent(DateTime.Now, martijn, "staff", "reason"));

            //make a DNF lap
            RiderReadyEvent dnfReady = new RiderReadyEvent(DateTime.Now, martijn, Guid.NewGuid(), "staff");

            ManualDNFEvent manualDnf = new ManualDNFEvent(dnfReady, "staff");

            Lap manualDnfLap = new Lap(manualDnf);

            //a dnf lap should always be slower (bigger) than a finished with DSQ lap
            Assert.AreEqual(1, manualDnfLap.CompareTo(dsqLap));
            Assert.AreEqual(-1, dsqLap.CompareTo(manualDnfLap));
        }
    }
}
