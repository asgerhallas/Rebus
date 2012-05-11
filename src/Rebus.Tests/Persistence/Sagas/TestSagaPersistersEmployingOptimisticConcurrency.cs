using System;
using NUnit.Framework;
using Rebus.Tests.Persistence.Sagas.Factories;

namespace Rebus.Tests.Persistence.Sagas
{
    [TestFixture(typeof(MongoDbSagaPersisterFactory), Category = TestCategories.Mongo)]
    public class TestSagaPersistersEmployingOptimisticConcurrency<TFactory> : TestSagaPersistersBase<TFactory> where TFactory : ISagaPersisterFactory
    {
        [Test]
        public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdatingFindingBySomeProperty()
        {
            var indexBySomeString = new[] { "SomeString" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
            Persister.Save(simpleSagaData, indexBySomeString);

            var sagaData1 = Persister.Find<SimpleSagaData>("SomeString", "hello world!");
            sagaData1.SomeString = "I changed this on one worker";

            EnterAFakeMessageContext();

            var sagaData2 = Persister.Find<SimpleSagaData>("SomeString", "hello world!");
            sagaData2.SomeString = "I changed this on another worker";
            Persister.Save(sagaData2, indexBySomeString);

            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => Persister.Save(sagaData1, indexBySomeString));
        }

        [Test]
        public void UsesOptimisticLockingAndDetectsRaceConditionsWhenUpdatingFindingById()
        {
            var indexBySomeString = new[] { "Id" };
            var id = Guid.NewGuid();
            var simpleSagaData = new SimpleSagaData { Id = id, SomeString = "hello world!" };
            Persister.Save(simpleSagaData, indexBySomeString);

            var sagaData1 = Persister.Find<SimpleSagaData>("Id", id);
            sagaData1.SomeString = "I changed this on one worker";

            EnterAFakeMessageContext();

            var sagaData2 = Persister.Find<SimpleSagaData>("Id", id);
            sagaData2.SomeString = "I changed this on another worker";
            Persister.Save(sagaData2, indexBySomeString);

            ReturnToOriginalMessageContext();

            Assert.Throws<OptimisticLockingException>(() => Persister.Save(sagaData1, indexBySomeString));
        }
    }
}