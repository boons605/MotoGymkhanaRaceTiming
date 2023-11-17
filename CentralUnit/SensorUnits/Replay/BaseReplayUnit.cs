using Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SensorUnits.Simulation
{
    /// <summary>
    /// This class provides base functionality to simulate a sensor based on a RaceSummary
    /// </summary>
    public abstract class BaseReplayUnit<T> where T : RaceEvent
    {
        protected Queue<T> eventsToReplay;
        protected RaceSummary race;

        public BaseReplayUnit(RaceSummary race)
        {
            this.race = race;
        }

        /// <summary>
        /// Analyze the race provided in the constructor and decide which events to replay
        /// </summary>
        public abstract void Initialize();

        public async Task Run(CancellationToken token)
        {
            int milliSecondsToWait = (int)(eventsToReplay.Peek().Time - race.Events[0].Time).TotalMilliseconds;

            while (!token.IsCancellationRequested)
            {
                //this is not going to be accurate, depends on windows task scheduler. Best we can hope for is 15ms accuracy. Worst case could be seconds
                await Task.Delay(milliSecondsToWait);

                T toReplay = eventsToReplay.Dequeue();
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
        /// Performs whatever actions are necessery to simulate that the provided event happened
        /// Should be able to handle every kind of event returned by <see cref="FilterEvents(RaceSummary)"/>
        /// </summary>
        /// <param name="raceEvent"></param>
        protected abstract void Replay(T raceEvent);
    }
}
