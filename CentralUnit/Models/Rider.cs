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
        private string name;

        /// <summary>
        /// The beacon that belongs to this rider.
        /// </summary>
        private Beacon beacon;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rider"/> class with a name an no beacon assigned yet.
        /// </summary>
        /// <param name="name">The rider name.</param>
        public Rider(string name) : this(name, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rider"/> class with a name and a beacon to identify the rider.
        /// </summary>
        /// <param name="name">The name of the rider.</param>
        /// <param name="beacon">The <see cref="Beacon"/> belonging to the rider.</param>
        public Rider(string name, Beacon beacon)
        {
            this.name = name;
            this.beacon = beacon;
        }

        /// <summary>
        /// Gets the rider name.
        /// </summary>
        public string Name { get => this.name; }

        /// <summary>
        /// Gets or sets the <see cref="Beacon"/> belonging to this rider.
        /// </summary>
        public Beacon Beacon { get => this.beacon; set => this.beacon = value; }
    }
}
