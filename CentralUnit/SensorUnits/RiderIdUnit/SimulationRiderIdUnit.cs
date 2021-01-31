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

        private readonly bool start;

        public string SensorId => start ? "startUnit" : "endUnit";

        /// <summary>
        /// Creates a new Rdier id unit that simulates the events fom the provided race
        /// </summary>
        /// <param name="start">When true this senor will simulate events for riders entering the track, when false for leaving the track</param>
        public SimulationRiderIdUnit(bool start, RaceSummary race)
            :base(race)
        {
            this.start = start;
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

        protected override Stack<RaceEvent> FilterEvents(RaceSummary race)
        {
            if(start)
                return new Stack<RaceEvent>(race.Events.Where(r => r is EnteredEvent));
            else
                return new Stack<RaceEvent>(race.Events.Where(r => r is LeftEvent));

        }

        protected override void Replay(RaceEvent raceEvent)
        {
            OnRiderId?.Invoke(this, new RiderIdEventArgs(raceEvent.Rider, raceEvent.Time, SensorId));
        }
    }
}
