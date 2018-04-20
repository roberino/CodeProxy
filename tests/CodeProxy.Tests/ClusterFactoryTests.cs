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
        public void WhenGivenAsyncComponentWithVirtualMethod_ThenCanCreateCluster()
        {
            var component1 = new Component();
            var component2 = new Component();

            var cluster = ClusterExtensions.CreateCluster(component1, component2);

            var instance = cluster.Implementation;

            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public async Task WhenComponentMethodCalled_ThenFirstComponentInvoked()
        {
            var component1 = new Component("a");
            var component2 = new Component("b");

            var cluster = ClusterExtensions.CreateCluster<IComponent>(component1, component2);

            var instance = cluster.Implementation;

            var val = await instance.GetValueAsync(1);

            Assert.That(val, Is.EqualTo("a-1"));
        }

        [Test]
        public async Task WhenFirstComponentFails_ThenSecondComponentInvoked()
        {
            var component1 = new Component("a", fail: true);
            var component2 = new Component("b");

            var cluster = ClusterExtensions.CreateCluster<IComponent>(component1, component2);

            var instance = cluster.Implementation;

            var val = await instance.GetValueAsync(1);

            Assert.That(val, Is.EqualTo("b-1"));
        }

        [Test]
        public async Task WhenVoidAsyncMethodCalled_ThenComponentInvoked()
        {
            var component1 = new Component();

            var cluster = ClusterExtensions.CreateCluster<IComponent>(component1);

            await cluster.Implementation.ActAsync("x");

            Assert.That(cluster.Implementation.State, Is.EqualTo("x"));
        }

        [Test]
        public void WhenNonAsyncMethodCalled_ThenResultReturned()
        {
            var component1 = new Component();

            var cluster = ClusterExtensions.CreateCluster<IComponent>(component1);

            var i = cluster.Implementation.GetValue(5);

            Assert.That(i, Is.EqualTo(10));
        }

        public interface IComponent
        {
            Task<string> GetValueAsync(int x);
            Task ActAsync(object state);
            int GetValue(int x);
            object State { get; }
        }

        public class Component : IComponent
        {
            private readonly string _name;
            private readonly bool _fail;

            public Component(string name = "a", bool fail = false)
            {
                _name = name;
                _fail = fail;
            }

            public Task<string> GetValueAsync(int x)
            {
                if (_fail) throw new InvalidOperationException();

                return Task.FromResult($"{_name}-{x}");
            }

            public async Task ActAsync(object state)
            {
                await Task.Delay(10);

                State = state;
            }

            public int GetValue(int x)
            {
                if (_fail) throw new InvalidOperationException();

                return x * 2;
            }

            public object State { get; private set; }
        }
    }
}
