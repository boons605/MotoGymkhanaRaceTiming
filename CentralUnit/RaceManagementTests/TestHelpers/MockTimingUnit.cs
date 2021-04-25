using System;
using System.Collections.Generic;
using System.Text;
using DisplayUnit;
using SensorUnits.TimingUnit;

namespace RaceManagementTests.TestHelpers
{
    public class MockTimingUnit : ITimingUnit, IDisplayUnit
    {
        public int StartId => throw new NotImplementedException();

        public int EndId => throw new NotImplementedException();

        public event EventHandler<TimingTriggeredEventArgs> OnTrigger;

        public int CurrentDisplay { get; private set; }

        public void EmitTriggerEvent(long microseconds, string unitId, int gateId, DateTime received) => OnTrigger.Invoke(this, new TimingTriggeredEventArgs(microseconds, unitId, gateId, received));

        public void SetDisplayTime(int milliSeconds, int secondsToClear)
        {
            CurrentDisplay = milliSeconds;
        }
    }
}
