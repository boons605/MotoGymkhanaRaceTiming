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
        public void AddThrowsForDuplicateRider()
        {
            RiderCollection subject = new RiderCollection();
            Rider toAdd = new Rider($"0", Guid.NewGuid());
            subject.Add(toAdd);

            Assert.ThrowsException<ArgumentException>(() => subject.Add(toAdd));
            // the assertion message comes from the dict implementation so we do not assert on it
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

        [TestMethod]
        public void RemoveRiderRemovesFirstRider()
        {
            RiderCollection subject = new RiderCollection();
            List<Rider> riders = new List<Rider>();

            for (int i = 0; i < 10; i++)
            {
                Rider toAdd = new Rider($"{i}", Guid.NewGuid());
                subject.Add(toAdd);
                riders.Add(toAdd);
            }

            for (int i = 0; i < 10; i++)
            {
                subject.Remove(riders[i].Id);

                List<Rider> returned = subject.Values.ToList();

                CollectionAssert.AreEqual(riders.Skip(i+1).ToList(), returned);
            }
        }

        [TestMethod]
        public void RemoveRiderRemovesLastRider()
        {
            RiderCollection subject = new RiderCollection();
            List<Rider> riders = new List<Rider>();

            for (int i = 0; i < 10; i++)
            {
                Rider toAdd = new Rider($"{i}", Guid.NewGuid());
                subject.Add(toAdd);
                riders.Add(toAdd);
            }

            for (int i = 9; i > -1; i--)
            {
                subject.Remove(riders[i].Id);

                List<Rider> returned = subject.Values.ToList();

                CollectionAssert.AreEqual(riders.Take(i).ToList(), returned);
            }
        }

        [TestMethod]
        public void RemoveRiderRemovesMiddleRider()
        {
            RiderCollection subject = new RiderCollection();
            List<Rider> riders = new List<Rider>();

            for (int i = 0; i < 10; i++)
            {
                Rider toAdd = new Rider($"{i}", Guid.NewGuid());
                subject.Add(toAdd);
                riders.Add(toAdd);
            }

            while(subject.Count > 0)
            {
                subject.Remove(riders[subject.Count/2].Id);
                riders.Remove(riders[riders.Count/2]);

                List<Rider> returned = subject.Values.ToList();

                CollectionAssert.AreEqual(riders, returned);
            }
        }

        [TestMethod]
        public void ChangePositionWorksForAllindicesInRange()
        {
            // try all possible combinations of source and target positions
            for (int source = 0; source<10; source++)
            {
                for(int target = 0; target<10; target++)
                {
                    RiderCollection subject = new RiderCollection();
                    List<Rider> riders = new List<Rider>();

                    for (int i = 0; i < 10; i++)
                    {
                        Rider toAdd = new Rider($"{i}", Guid.NewGuid());
                        subject.Add(toAdd);
                        riders.Add(toAdd);
                    }

                    subject.ChangePosition(riders[source].Id, target);

                    List<Rider> returned = subject.Values.ToList();

                    Assert.AreEqual(riders[source], returned[target]);
                }
            }
        }

        [TestMethod]
        public void ChangePositionThrowsWithNegativeIndex()
        {
            RiderCollection subject = new RiderCollection();
            
            Rider toAdd = new Rider("0", Guid.NewGuid());
            subject.Add(toAdd);
            
            ArgumentException e = Assert.ThrowsException<ArgumentException>(() => subject.ChangePosition(toAdd.Id, -1));

            Assert.AreEqual("Cannot assign a rider to a negative starting position", e.Message);
        }

        [TestMethod]
        public void ChangePositionThrowsWithTooLargeIndex()
        {
            RiderCollection subject = new RiderCollection();

            Rider toAdd = new Rider("0", Guid.NewGuid());
            subject.Add(toAdd);

            ArgumentException e = Assert.ThrowsException<ArgumentException>(() => subject.ChangePosition(toAdd.Id, 1));

            Assert.AreEqual("Cannot assign a rider to a starting position more than the (number of riders - 1), that would leave a gap in the grid", e.Message);
        }

        [TestMethod]
        public void ChangePositionThrowsWithUnkownRider()
        {
            RiderCollection subject = new RiderCollection();

            Rider toAdd = new Rider("0", Guid.NewGuid());
            subject.Add(toAdd);

            Guid unknown = Guid.NewGuid();
            KeyNotFoundException e = Assert.ThrowsException<KeyNotFoundException>(() => subject.ChangePosition(unknown, 1));

            Assert.AreEqual($"Rider {unknown} not found in collection", e.Message);
        }
    }
}
