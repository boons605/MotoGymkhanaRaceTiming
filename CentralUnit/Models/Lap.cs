using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Lap: IComparable<Lap>
    {
        public RaceEvent End { get; private set;}

        public Rider Rider => End.Rider;

        public long LapTime 
        {
            get
            {
                if(End is FinishedEvent f)
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
            End = finish;
        }

        public Lap(UnitDNFEvent dnf)
        {
            End = dnf;
        }

        public Lap(ManualDNFEvent dnf)
        {
            End = dnf;
        }

        public int CompareTo(Lap other)
        {
            {
                if (this.End is FinishedEvent fa && other.End is FinishedEvent fb)
                {
                    //when both laps have actually finished compare lap times
                    return fa.LapTime.CompareTo(fb.LapTime);
                }
                else if (this.End is FinishedEvent)
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
