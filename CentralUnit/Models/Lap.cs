// <copyright file="Lap.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Models
{
    /// <summary>
    /// This class represents a completed lap including penalties.
    /// A lap is completed when a rider triggers the end timing gate or gets disqualified
    /// </summary>
    public class Lap : IComparable<Lap>
    {
        /// <summary>
        /// All penalties applied to this lap
        /// </summary>
        private List<PenaltyEvent> penalties = new List<PenaltyEvent>();

        /// <summary>
        /// The timing event that ended this lap
        /// </summary>
        public RaceEvent End { get; private set; }

        /// <summary>
        /// The event which disqualified this lap
        /// </summary>
        public DSQEvent Dsq { get; private set; }

        /// <summary>
        /// The rider that drove this lap
        /// </summary>
        public Rider Rider => End.Rider;

        /// <summary>
        /// Whether this lap has been disqualified
        /// </summary>
        public bool Disqualified => Dsq != null;

        /// <summary>
        /// A copy of the list of penalties applied to this lap.
        /// If you modify a penalty here that will affect the lap
        /// </summary>
        public List<PenaltyEvent> Penalties => penalties.ToList();

        /// <summary>
        /// Initializes a new instance of the <see cref="Lap" /> class.
        /// ONLY FOR USE BY JSON DESERIALIZATION
        /// </summary>
        /// <param name="end"></param>
        /// <param name="dsq"></param>
        [JsonConstructor]
        public Lap(RaceEvent end, DSQEvent dsq)
        {
            End = end;
            Dsq = dsq;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lap" /> class.
        /// This lap was successfully completed by triggering the end timing gate
        /// </summary>
        /// <param name="finish"></param>
        public Lap(FinishedEvent finish)
        {
            End = finish;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lap" /> class.
        /// The rider did not properly finish this lap so staff ended it manually
        /// </summary>
        /// <param name="dnf"></param>
        public Lap(ManualDNFEvent dnf)
        {
            End = dnf;
        }

        /// <summary>
        /// Applies the given penalties to the lap
        /// </summary>
        /// <param name="penalties"></param>
        public void AddPenalties(List<PenaltyEvent> penalties)
        {
            this.penalties.AddRange(penalties);
        }

        /// <summary>
        /// Get lap time in microseconds. Laps with a DNF get a lap time of -1
        /// Laps with a DSQ get a normally calculated lap time
        /// </summary>
        /// <param name="includePenalties">if true add the lap penalties to the calculated lap time</param>
        /// <returns></returns>
        public long GetLapTime(bool includePenalties = true)
        {
            if (End is FinishedEvent f)
            {
                if (includePenalties)
                {
                    return f.LapTime + (penalties.Sum(p => p.Seconds) * 1000000); // 1 penalty second is 1000000 microseconds
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

        /// <summary>
        /// Signals the the rider was disqualified
        /// </summary>
        /// <param name="dsq"></param>
        public void SetDsq(DSQEvent dsq)
        {
            if (this.Disqualified)
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
        /// Compares two laps by their lap time. Maps the regular returns of compare methods to lap times.
        /// Laps with a DNF are always slower than finished laps. There is no defined ordering between laps with a DNF
        /// </summary>
        /// <param name="other"></param>
        /// <param name="includePenalties"></param>
        /// <returns>-1 this lap is faster, 1 the other lap is faster, 0 the laps are equal</returns>
        public int CompareTo(Lap other, bool includePenalties)
        {
            //both have finished
            if (this.End is FinishedEvent && other.End is FinishedEvent) 
            {
                //if both laps are dsq or both are not dsq lap time decides ordering
                if (this.Disqualified == other.Disqualified) 
                {
                    //when both laps have actually finished compare lap times
                    return this.GetLapTime(includePenalties).CompareTo(other.GetLapTime(includePenalties));
                }
                else
                {
                    //disqualified is slower than not disqualified
                    if (this.Disqualified) 
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            else if (this.End is FinishedEvent) 
            {
                //this has finished, other DNF
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
