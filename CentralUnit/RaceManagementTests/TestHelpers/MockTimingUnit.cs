using System;
using System.Collections.Generic;
using System.Text;
using SensorUnits.TimingUnit;

namespace RaceManagementTests.TestHelpers
{
    public class MockTimingUnit : ITimingUnit
    {
        public event EventHandler<TimingTriggeredEventArgs> OnTrigger;

        public void EmitTriggerEvent(long microseconds, string unitId, int gateId, DateTime received) => OnTrigger.Invoke(this, new TimingTriggeredEventArgs(microseconds, unitId, gateId, received));
    }
}
