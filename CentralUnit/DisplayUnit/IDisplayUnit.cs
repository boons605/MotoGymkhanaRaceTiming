using System;
using System.Collections.Generic;
using System.Text;

namespace DisplayUnit
{
    public interface IDisplayUnit
    {

        /// <summary>
        /// Set the time on a display unit to the lap duration in milliseconds.
        /// </summary>
        /// <param name="milliSeconds">The lap duration in milliseconds</param>
        void SetDisplayTime(int milliSeconds);
    }
}
