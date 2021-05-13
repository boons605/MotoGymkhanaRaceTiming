using System;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    public class Lap: IComparable<Lap>
    {
        private List<PenaltyEvent> penalties = new List<PenaltyEvent>();

        public RaceEvent End { get; private set; }
        public DSQEvent Dsq { get; private set; }

        public Rider Rider => End.Rider;

        public bool Disqualified => Dsq != null;
        public List<PenaltyEvent> Penalties => penalties.ToList();

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

        public void AddPenalties(List<PenaltyEvent> penalties)
        {
            this.penalties.AddRange(penalties);
        }

        public void SetDsq(DSQEvent dsq)
        {
            if(this.Disqualified)
            {
                throw new InvalidOperationException("Cannot set the DSQ event of a lap that already has one");
            }
            else
            {
                Dsq = dsq;
            }
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
