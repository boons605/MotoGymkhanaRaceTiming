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
        /// The unique id for this rider.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rider"/> class with a name and a beacon to identify the rider.
        /// </summary>
        /// <param name="name">The name of the rider.</param>
        /// <param name="id">The unique id associated with this rider</param>
        public Rider(string name, Guid id)
        {
            Name = name;
            Id = id;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Rider {this.Name} with id {this.Id}";
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                if(obj is Rider)
                {
                    Rider other = (Rider)obj;
                    if (this.Name == other.Name)
                    {
                        if (this.Id != null)
                        {
                            return this.Id.Equals(other.Id);
                        }
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int retVal = 0;
            if (!string.IsNullOrEmpty(this.Name))
            {
                retVal = Name.GetHashCode();
            }

            if (this.Id != null)
            {
                retVal ^= Id.GetHashCode();
            }

            return retVal;
        }
    }
}
