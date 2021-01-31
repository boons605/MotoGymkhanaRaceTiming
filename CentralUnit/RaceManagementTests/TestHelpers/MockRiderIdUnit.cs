using System;
using System.Collections.Generic;
using Models;
using SensorUnits.RiderIdUnit;

namespace RaceManagementTests.TestHelpers
{
    /// <summary>
    /// This class allows test code to emit events at will
    /// </summary>
    public class MockRiderIdUnit : IRiderIdUnit
    {
        public string SensorId { get; private set; }

        public event EventHandler<RiderIdEventArgs> OnRiderId;
        public event EventHandler<RiderIdEventArgs> OnRiderExit;

        public MockRiderIdUnit(string id)
        {
            SensorId = id;
        }

        public void AddKnownRiders(List<Rider> riders)
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

        public void EmitIdEvent(Rider rider, DateTime received) => OnRiderId.Invoke(this, new RiderIdEventArgs(rider, received, SensorId));
        public void EmitExitEvent(Rider rider, DateTime received) => OnRiderId.Invoke(this, new RiderIdEventArgs(rider, received, SensorId));
    }
}
