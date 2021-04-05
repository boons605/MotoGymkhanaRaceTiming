using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Lap
    {
        RaceEvent end;

        public Rider Rider => end.Rider;

        public Lap(FinishedEvent finish)
        {
            end = finish;
        }

        public Lap(DNFEvent dnf)
        {
            end = dnf;
        }

        public static bool operator <=(Lap a, Lap b)
        {
            if(a.end is FinishedEvent fa && b.end is FinishedEvent fb)
            {
                //when both laps have actually finished compare lap times
                return fa.LapTime <= fb.LapTime;
            }
            else if (a.end is FinishedEvent)
            {
                //if only lap a has finished a is faster
                return true;
            }
            else
            {
                //if only b has finished b is faster, or both are DNF and then order is irrelevant
                return false;
            }
        }

        public static bool operator >=(Lap a, Lap b) => b <= a
    }
}
