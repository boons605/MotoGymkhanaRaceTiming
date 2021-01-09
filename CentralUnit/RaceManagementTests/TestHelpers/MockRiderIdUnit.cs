using System;
using System.Collections.Generic;
using RiderIdUnit;

namespace RaceManagementTests.TestHelpers
{
    /// <summary>
    /// This class allows test code to emit events at will
    /// </summary>
    public class MockRiderIdUnit : IRiderIdUnit
    {
        public event EventHandler<RiderIdEventArgs> OnRiderId;
        public event EventHandler<RiderIdEventArgs> OnRiderExit;

        public void AddKnownRiders(List<(byte[] sensorId, string name)> riders)
        {
            throw new NotImplementedException();
        }

        public void ClearKnownRiders()
        {
            throw new NotImplementedException();
        }

        public void RemoveKnownRider(string name)
        {
            throw new NotImplementedException();
        }

        public void EmitIdEvent(string riderName, byte[] sensorId, DateTime received, string unitId) => OnRiderId.Invoke(this, new RiderIdEventArgs(riderName, sensorId, received, unitId));
        public void EmitExitEvent(string riderName, byte[] sensorId, DateTime received, string unitId) => OnRiderId.Invoke(this, new RiderIdEventArgs(riderName, sensorId, received, unitId));
    }
}
