using Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WebPusher.WebInterfaces
{
    /// <summary>
    /// Defines the interface to a web service that wants to receive updates
    /// </summary>
    public interface IWebInterface
    {
        /// <summary>
        /// Signals that a rider has triggered the timing equipment at the start box
        /// Must be robust to receiving this call for a rider thats already driving a lap
        /// </summary>
        /// <param name="start"></param>
        public Task StartLap(IdEvent start);

        /// <summary>
        /// Signals that a lap has been completed.
        /// Must also apply any penalties that are attached to the lap (DNF, DSQ or time penalties)
        /// Can also be used without calling <see cref="StartLap(IdEvent)"/> first
        /// </summary>
        /// <param name="lap"></param>
        public Task NewTime(Lap lap);

        /// <summary>
        /// Signals that a rider has received a DNF or DSQ. Ends the lap, must have received a penalty first.
        /// Useful if you cannot wait until you receive a Lap object from the WebAPI
        /// </summary>
        public Task EndLap(IdEvent end);

        /// <summary>
        /// Adds a penalty to a lap that was posted previously with <see cref="NewTime(Lap)"/>
        /// Or to a lap in progress with <see cref="StartLap(IdEvent)"/>.
        /// Penalties on lap object are time penalties, DNF of DSQ. If a DNF or DSQ is recorded, the lap also ends
        /// </summary>
        /// <param name="lap"></param>
        public Task AddPenalty(Lap lap);

        /// <summary>
        /// Clear everything thats currently displayed
        /// </summary>
        public Task Clear();
    }
}
