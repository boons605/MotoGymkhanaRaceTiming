using System;
using System.Collections.Generic;
using System.Linq;
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

        public Beacon Closest => new Beacon(new byte[] { 0, 0, 0, 0, 0, 0 }, 0);

        public event EventHandler<RiderIdEventArgs> OnRiderId;
        public event EventHandler<RiderIdEventArgs> OnRiderExit;

        public List<Rider> KnownRiders = new List<Rider>();

        public MockRiderIdUnit(string id)
        {
            UnitId = id;
        }

        public void AddKnownRiders(List<Rider> riders)
        {
            KnownRiders.AddRange(riders);
        }

        public void ClearKnownRiders()
        {
            KnownRiders.Clear();
        }

        public void RemoveKnownRider(string name)
        {
            Rider toRemove = KnownRiders.Where(r => r.Name == name).FirstOrDefault();

            if (toRemove != null)
            {
                KnownRiders.Remove(toRemove);
            }
        }

        public void EmitIdEvent(Rider rider, DateTime received) => OnRiderId.Invoke(this, new RiderIdEventArgs(rider, received, UnitId, Direction.Enter));
        public void EmitExitEvent(Rider rider, DateTime received) => OnRiderExit.Invoke(this, new RiderIdEventArgs(rider, received, UnitId, Direction.Exit));
    }
}
