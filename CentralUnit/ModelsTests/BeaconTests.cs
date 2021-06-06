using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Models;
using System.Linq;

namespace ModelsTests
{
    [TestClass]
    public class BeaconTests
    {
        [TestMethod]
        public void IdentifierText_ShouldBeSymmetric()
        {
            Random random = new Random(1000);

            for(int i = 0; i<100; i++)
            {
                byte[] identifier = new byte[6];

                random.NextBytes(identifier);

                string serialized = Beacon.IdentifierBytesToText(identifier);

                byte[] deserialized = Beacon.TextToMacBytes(serialized);

                Assert.IsTrue(identifier.Zip(deserialized).All(pair => pair.First == pair.Second));
            }
        }
    }
}
