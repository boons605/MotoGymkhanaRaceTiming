using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SensorUnits.RiderIdUnit
{
    public class SimulationRiderIdUnit : Simulation.BaseSimulationUnit<RaceEvent>, IRiderIdUnit
    {
        public event EventHandler<RiderIdEventArgs> OnRiderId;
        public event EventHandler<RiderIdEventArgs> OnRiderExit;

        public string UnitId { get; private set; }

        /// <summary>
        /// Creates a new Rider id unit that simulates the events fom the provided race
        /// </summary>
        /// <param name="start">When true this sensor will simulate events for riders entering the track, when false for leaving the track</param>
        public SimulationRiderIdUnit(string unitId, RaceSummary race)
            : base(race)
        {
            this.UnitId = unitId;
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

        public override void Initialize()
        {
            eventsToReplay = new Queue<RaceEvent>(race.Events.Where(r => r is IdEvent id && id.UnitId == UnitId));
        }

        protected override void Replay(RaceEvent raceEvent)
        {
            if (raceEvent is IdEvent id)
            {
                OnRiderId?.Invoke(this, new RiderIdEventArgs(id.Rider, id.Time, id.UnitId, id.IdType));
            }
            else
            {
                throw new ArgumentException($"Cannot replay non IdEvent: {raceEvent.GetType()}");
            }
        }
    }
}