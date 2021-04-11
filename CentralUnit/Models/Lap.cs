using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Lap: IComparable<Lap>
    {
        RaceEvent end;

        public Rider Rider => end.Rider;

        public long LapTime 
        {
            get
            {
                if(end is FinishedEvent f)
                {
                    return f.LapTime;
                }
                else
                {
                    return -1;
                }
            }
        }

        public Lap(FinishedEvent finish)
        {
            end = finish;
        }

        public Lap(DNFEvent dnf)
        {
            end = dnf;
        }

        public int CompareTo(Lap other)
        {
            {
                if (this.end is FinishedEvent fa && other.end is FinishedEvent fb)
                {
                    //when both laps have actually finished compare lap times
                    return fa.LapTime.CompareTo(fb.LapTime);
                }
                else if (this.end is FinishedEvent)
                {
                    return -1;
                }
                else
                {
                    //if only b has finished b is faster, or both are DNF and then order is irrelevant
                    return 1;
                }
            }
        }
    }
}
