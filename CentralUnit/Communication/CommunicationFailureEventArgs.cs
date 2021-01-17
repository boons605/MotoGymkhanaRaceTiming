// <copyright file="CommunicationFailureEventArgs.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Communication
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Type of communication failure.
    /// </summary>
    public enum FailureType
    {
        /// <summary>
        /// Communication channel got disconnected
        /// </summary>
        Disconnect,
        
        /// <summary>
        /// Communication timeout occurred.
        /// </summary>
        Timeout,

        /// <summary>
        /// Generic failure occurred.
        /// </summary>
        Generic
    }
    
    /// <summary>
    /// Event args to pass data about a communication failure with a Communication Failure event.
    /// </summary>
    public class CommunicationFailureEventArgs : EventArgs
    {
        /// <summary>
        /// The type of failure.
        /// </summary>
        public readonly FailureType Failure;

        /// <summary>
        /// Message about the failure.
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationFailureEventArgs" /> class
        /// </summary>
        /// <param name="failureType">The type of failure.</param>
        /// <param name="message">Message about the failure.</param>
        public CommunicationFailureEventArgs(FailureType failureType, string message)
        {
            this.Failure = failureType;
            this.Message = message;
        }
    }
}
