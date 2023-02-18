using DisplayUnit;
using Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SensorUnits.TimingUnit
{
    public class SimulationTimingUnit : ITimingUnit, IDisplayUnit
    {
        public event EventHandler<TimingTriggeredEventArgs> OnTrigger;

        public SimulationData Data { get; private set; }

        public int CurrentDisplay { get; private set; }

        public int StartId => Data.StartGateId;

        public int EndId => Data.EndGateId;

        public SimulationTimingUnit(SimulationData data)
        {
            Data = data;
        }

        /// <summary>
        /// Runs the simulation after the provided delay
        /// </summary>
        /// <param name="token"></param>
        /// <param name="delayMilliseconds"></param>
        /// <param name="overrideEventDelayMilliseconds">if provided wait this amount of milliseconds in between events instead of the value provided in the events themselves</param>
        /// <returns></returns>
        public async Task Run(CancellationToken token, int delayMilliseconds, int? overrideEventDelayMilliseconds)
        {
            await Task.Delay(delayMilliseconds);

            int milliSecondsToWait = delayMilliseconds;

            for(int i = 0; i<Data.Events.Count; i++)
            {
                SimulatedTimingEvent currentEvent = Data.Events[i];

                await Task.Delay(overrideEventDelayMilliseconds ?? currentEvent.MillisecondsFromSimStart);

                OnTrigger?.Invoke(this, new TimingTriggeredEventArgs(currentEvent.Microseconds, "SimulatedTimer", currentEvent.GateId, DateTime.Now));

                if(token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();

                    return;
                }
            }
        }

        public void SetDisplayTime(int milliSeconds)
        {
            CurrentDisplay = milliSeconds;
        }
    }
}
