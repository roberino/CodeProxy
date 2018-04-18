using CodeProxy.FailSafe;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CodeProxy.Tests
{
    [TestFixture]
    public class ClusterFactoryTests
    {
        [Test]
        public void WhenGivenAsyncComponent_ThenCanCreateCluster()
        {
            var component1 = new Component();
            var component2 = new Component();

            var cluster = ClusterExtensions.CreateCluster(component1, component2);

            var instance = cluster.Implementation;

            Assert.That(instance, Is.Not.Null);
        }

        public interface IComponent
        {
            Task<int> GetValue(double x);
        }

        public class Component : IComponent
        {
            public Task<int> GetValue(double x)
            {
                return Task.FromResult((int)x);
            }
        }
    }
}
