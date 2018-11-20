using System;
using NUnit.Framework;

namespace CodeProxy.Tests
{
    [TestFixture]
    public class InterceptionExtensionsTests
    {
        [Test]
        public void Does_WhenGivenImplementation_ThenImplemenationUsed()
        {
            var fact = new ClassFactory<IX>();
            var instance = fact.BuildInstance();

            instance.Run().Does(c => 123);

            var x = instance.Run();

            Assert.That(x, Is.EqualTo(123));
        }

        [Test]
        public void Does_WhenOverrideMethodImplementation_ThenImplemenationUsed()
        {
            var fact = new ClassFactory<IX>();
            var instance = fact.BuildInstance();

            instance.Run().Does(c => 123);
            instance.Run().Does(c => 456);

            var x = instance.Run();

            Assert.That(x, Is.EqualTo(456));
        }
        
        [Test]
        public void Does_WhenMethodImplementationWithoutArgs_ThenImplemenationUsed()
        {
            var fact = new ClassFactory<IX>();
            var instance = fact.BuildInstance();

            instance.Run().Does(() => 123);

            var x = instance.Run();

            Assert.That(x, Is.EqualTo(123));
        }

        [Test]
        public void Does_WhenOverridePropertyImplementation_ThenImplemenationUsed()
        {
            var fact = new ClassFactory<IX>();
            var instance = fact.BuildInstance();
            
            instance.Property1.Does(() => "hi");

            var x = instance.Property1;

            Assert.That(x, Is.EqualTo("hi"));
        }

        [Test]
        public void Does_WhenImplementationContainsArgs_ThenArgsAvailableToImplementingFunc()
        {
            var fact = new ClassFactory<IX>();
            var instance = fact.BuildInstance();

            instance.Act(123).Does(a => Convert.ToInt32(a["x"]));

            var x = instance.Act(456);

            Assert.That(x, Is.EqualTo(456));
        }

        [Test]
        public void Does_WhenImplementTwoMethods_ThenBothMethodsBehaveCorrectly()
        {
            var fact = new ClassFactory<IX>();
            var instance = fact.BuildInstance();

            instance.Run().Does(_ => 123);
            instance.Act(0).Does(a => Convert.ToInt32(a["x"]));
        
            var x = instance.Act(456);
            var y = instance.Run();

            Assert.That(x, Is.EqualTo(456));
            Assert.That(y, Is.EqualTo(123));
        }

        [Test]
        public void Does_WhenMethodThrowsException_ThenExceptionRaised()
        {
            var fact = new ClassFactory<IX>();
            var instance = fact.BuildInstance();

            instance.Run().Does(_ => throw new InvalidOperationException());

            Assert.Throws<InvalidOperationException>(() => instance.Run());
        }

        public interface IX
        {
            string Property1 { get; }

            int Run();

            int Act(double x);
        }
    }
}
