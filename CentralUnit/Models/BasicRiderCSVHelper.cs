// <copyright file="BasicRiderCSVHelper.cs" company="Moto Gymkhana">
//     Copyright (c) Moto Gymkhana. All rights reserved.
// </copyright>
namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using log4net;

    /// <summary>
    /// Basic CSV rider list parser.
    /// </summary>
    public class BasicRiderCSVHelper
    {
        /// <summary>
        /// Logger object used to display data in a console or file.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Parse a rider line.
        /// </summary>
        /// <param name="line">A rider line, semicolon separated: Guid;RiderName</param>
        /// <returns>A Rider object.</returns>
        public static Rider ParseRiderLine(string line)
        {
            Rider rider = null;

            if (!string.IsNullOrEmpty(line))
            {
                string[] elements = line.Split(';');

                Guid id = Guid.Parse(elements[0]);                

                if (elements.Length == 2)
                {
                    rider = new Rider(elements[2], id);
                }
                else
                {
                    throw new FormatException($"Could not parse rider, expected 2 fields in line but found {elements.Length}");
                }

                return rider;
            }
            else
            {
                throw new ArgumentNullException(nameof(line));
            }
        }
    }
}
