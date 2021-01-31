// <copyright file="Rider.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Contains rider information, like the rider name and the beacon belonging to the rider.
    /// </summary>
    public class Rider
    {
        /// <summary>
        /// The rider name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The beacon that belongs to this rider.
        /// </summary>
        public Beacon Beacon { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rider"/> class with a name and a beacon to identify the rider.
        /// </summary>
        /// <param name="name">The name of the rider.</param>
        /// <param name="beacon">The <see cref="Beacon"/> belonging to the rider.</param>
        public Rider(string name, Beacon beacon)
        {
            Name = name;
            Beacon = beacon;
        }
    }
}
