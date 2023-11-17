using SensorUnits.StartLightUnit;
using System;
using System.Collections.Generic;
using System.Text;

namespace RaceManagementTests.TestHelpers
{
    public class MockStartLightUnit : IStartLightUnit
    {
        public StartLightColor CurrentColor { get; private set; }

        public void SetStartLightColor(StartLightColor color)
        {
            CurrentColor = color;
        }
    }
}
