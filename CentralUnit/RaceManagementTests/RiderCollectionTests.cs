using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DisplayUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Models.Config;
using RaceManagement;
using RaceManagementTests.TestHelpers;

namespace RaceManagementTests
{
    [TestClass]
    public class RiderCollectionTests
    {
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ValuesKeepsInsertOrder(bool indexer)
        {
            RiderCollection subject = new RiderCollection();

            for(int i = 0; i<10; i++)
            {
                Rider toAdd = new Rider($"{i}", Guid.NewGuid());
                if (indexer)
                {
                    subject[toAdd.Id] = toAdd;
                }
                else
                {
                    subject.Add(toAdd);
                }
            }

            List<Rider> returned = subject.Values.ToList();

            for(int i = 0; i<10; i++)
            {
                Assert.AreEqual(returned[i].Name, $"{i}");
            }

            Assert.AreEqual(subject.Count, 10);
        }

        [TestMethod]
        public void IndexerRetreivesCorrectRider()
        {
            RiderCollection subject = new RiderCollection();

            for (int i = 0; i < 10; i++)
            {
                subject.Add(new Rider($"{i}", Guid.NewGuid()));
            }

            List<Rider> returned = subject.Values.ToList();

            foreach (Rider rider in returned)
            {
                Assert.AreEqual(rider.Name, subject[rider.Id].Name);
                Assert.AreEqual(rider.Id, subject[rider.Id].Id);
            }
        }

        [TestMethod]
        public void RemoveReturnsFalseForUnknownRider()
        {
            RiderCollection subject = new RiderCollection();

            subject.Add(new Rider("known", Guid.NewGuid()));

            Assert.IsFalse(subject.Remove(Guid.NewGuid()));
            Assert.AreEqual(1, subject.Count);
            Assert.AreEqual(1, subject.Values.Count);
        }
    }
}
