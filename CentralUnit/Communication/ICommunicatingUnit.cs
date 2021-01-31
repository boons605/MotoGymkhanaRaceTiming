// <copyright file="ICommunicatingUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Communicating unit.
    /// </summary>
    public interface ICommunicatingUnit
    {
        /// <summary>
        /// Event raised when there is a communication failure in the unit.
        /// </summary>
        event EventHandler<CommunicationFailureEventArgs> CommunicationFailure;
    }
}
