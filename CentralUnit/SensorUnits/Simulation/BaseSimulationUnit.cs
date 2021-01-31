using Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SensorUnits.Simulation
{
    /// <summary>
    /// This class provides base functionality to simulate a sensor based on a RaceSummary
    /// </summary>
    public abstract class BaseSimulationUnit<T> where T : RaceEvent
    {
        private readonly Stack<T> eventsToReplay;
        private readonly double milliSecondsToStart;

        public BaseSimulationUnit(RaceSummary race)
        {
            eventsToReplay = FilterEvents(race);

            RaceEvent raceStart = race.Events[0];
            milliSecondsToStart = (eventsToReplay.Peek().Time - raceStart.Time).TotalMilliseconds;
        }

        public async Task Run(CancellationToken token)
        {
            int milliSecondsToWait = (int)milliSecondsToStart;
            
            while(!token.IsCancellationRequested)
            {
                //this is not going to be accurate, depends on windows task scheduler. Best we can hope for is 15ms accuracy. Worst case could be seconds
                await Task.Delay(milliSecondsToWait);

                T toReplay = eventsToReplay.Pop();
                Replay(toReplay);

                if (eventsToReplay.Count == 0)
                {
                    break;
                }
                else
                {
                    milliSecondsToWait = (int)(eventsToReplay.Peek().Time - toReplay.Time).TotalMilliseconds;
                }
            }
        }

        /// <summary>
        /// Extract all the events that this sensor should replay from the race
        /// </summary>
        /// <param name="race"></param>
        /// <returns>All the events that should be replayed so that popping returns events in chronological order</returns>
        protected abstract Stack<T> FilterEvents(RaceSummary race);

        /// <summary>
        /// Performs whatever actions are necessery to simulate that the provided event happened
        /// Should be able to handle every kind of event returned by <see cref="FilterEvents(RaceSummary)"/>
        /// </summary>
        /// <param name="raceEvent"></param>
        protected abstract void Replay(T raceEvent);
    }
}
