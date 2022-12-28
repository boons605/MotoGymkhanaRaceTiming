// <copyright file="IStartLightUnit.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace SensorUnits.StartLightUnit
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Enum listing the start light colors.
    /// </summary>
    public enum StartLightColor
    {
        /// <summary>
        /// Start light is off.
        /// </summary>
        OFF,

        /// <summary>
        /// Start light shows red color
        /// </summary>
        RED,

        /// <summary>
        /// Start light shows yellow color
        /// </summary>
        YELLOW,

        /// <summary>
        /// Start light shows green color
        /// </summary>
        GREEN
    }

    /// <summary>
    /// Interface for units controlling a start light.
    /// </summary>
    public interface IStartLightUnit
    {
        /// <summary>
        /// Set the start light color to the specified color.
        /// </summary>
        /// <param name="color">The desired color from <see cref="StartLightColor"/></param>
        void SetStartLightColor(StartLightColor color);
    }
}
