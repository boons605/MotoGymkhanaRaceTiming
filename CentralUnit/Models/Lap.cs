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

        /// <summary>
        /// Get lap time in microseconds. Laps with a DNF get a lap time of -1
        /// Laps with a DSQ get a normally calulcated lap time
        /// </summary>
        /// <param name="includePenalties">if true add the lap penalties to the calculated lap time</param>
        /// <returns></returns>
        public long GetLapTime(bool includePenalties=true)
        {
            if (End is FinishedEvent f)
            {
                if (includePenalties)
                {
                    return f.LapTime + (penalties.Sum(p => p.Seconds) * 1000000);//1 penalty second is 1000000 microseconds
                }
                else
                {
                    return f.LapTime;
                }
            }
            else
            {
                return -1;
            }
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

        /// <summary>
        /// Maps to <see cref="CompareTo(Lap, bool)"/> with includePenalties=true
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Lap other)
        {
            return CompareTo(other, true);
        }

        /// <summary>
        /// Compates two laps by their lap time. Maps the regular returns of compare methods to lap times.
        /// Laps with a DNF are always slower than finished laps. There is no defined ordering between laps with a DNF
        /// </summary>
        /// <param name="other"></param>
        /// <param name="includePenalties"></param>
        /// <returns>-1 this lap is fater, 1 the other lap is faster, 0 the laps are equal</returns>
        public int CompareTo(Lap other, bool includePenalties)
        {
            if (this.End is FinishedEvent fa && other.End is FinishedEvent fb)//both have finished
            {
                if (this.Disqualified == other.Disqualified)//if both laps are dsq or both are not dsq lap time decides ordering
                {
                    //when both laps have actually finished compare lap times
                    return this.GetLapTime(includePenalties).CompareTo(other.GetLapTime(includePenalties));
                }
                else
                {
                    if(this.Disqualified)//disqualified is slower than not disqualified
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            else if (this.End is FinishedEvent)//this has finished, other DNF
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
