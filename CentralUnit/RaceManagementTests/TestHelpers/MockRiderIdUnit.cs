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
        public string UnitId { get; private set; }

        public event EventHandler<RiderIdEventArgs> OnRiderId;
        public event EventHandler<RiderIdEventArgs> OnRiderExit;

        public MockRiderIdUnit(string id)
        {
            UnitId = id;
        }

        public void AddKnownRiders(List<Rider> riders)
        {
            //do nothing
        }

        public void ClearKnownRiders()
        {
            throw new NotImplementedException();
        }

        public void RemoveKnownRider(string name)
        {
            //do nothing
        }

        public void EmitIdEvent(Rider rider, DateTime received) => OnRiderId.Invoke(this, new RiderIdEventArgs(rider, received, UnitId, Direction.Enter));
        public void EmitExitEvent(Rider rider, DateTime received) => OnRiderId.Invoke(this, new RiderIdEventArgs(rider, received, UnitId, Direction.Exit));
    }
}
