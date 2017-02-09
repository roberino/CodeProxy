using NUnit.Framework;

namespace CodeProxy.Tests
{
    [TestFixture]
    public class ClassFactoryTests
    {
        [Test]
        public void Create_SimpleInterface()
        {
            var fact = new ClassFactory<X>();

            var instance = fact.CreateInstance();

            instance.ValueY = "a";

            Assert.That(instance.ValueY, Is.EqualTo("a"));
        }

        [Test]
        public void Create_SimpleInterface_Intercept()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertyImplementation((p, v) => v + "x");

            var instance = fact.CreateInstance();

            instance.ValueY = "a";

            Assert.That(instance.ValueY, Is.EqualTo("ax"));
        }

        [Test]
        public void Create_InterfaceWithMethods_NoInterception()
        {
            var fact = new ClassFactory<Y>();

            var instance = fact.CreateInstance();

            var y = instance.MethodY(12);

            Assert.That(y, Is.Null);
        }

        [Test]
        public void Create_InterfaceWithMethods_SingleMethodInterceptor()
        {
            var fact = new ClassFactory<Y>();

            fact.AddMethodImplementation((m, p) =>
            {
                return p["yp"].ToString();
            });

            var instance = fact.CreateInstance();

            var y = instance.MethodY(12);

            Assert.That(y, Is.EqualTo("12"));
        }

        [Test]
        public void Create_InterfaceWithMethods_SinglePrimativeMethodInterceptor()
        {
            var fact = new ClassFactory<Z>();

            fact.AddMethodImplementation((m, p) =>
            {
                return System.Convert.ToSingle(p["yp"]);
            });

            var instance = fact.CreateInstance();

            var y = instance.MethodZ(12);

            Assert.That(y, Is.EqualTo((float)12));
        }

        [Test]
        public void Create_InterfaceWithMethods_SingleVoidMethodInterceptor()
        {
            var fact = new ClassFactory<V>();
            var wasIntercepted = false;

            fact.AddMethodImplementation((m, p) =>
            {
                wasIntercepted = true;
                return p.Count;
            });

            var instance = fact.CreateInstance();

            instance.MethodV(12);

            Assert.That(wasIntercepted);
        }

        public interface X
        {
            string ValueY { get; set; }
        }

        public interface Y
        {
            string MethodY(int yp);
        }

        public interface Z
        {
            float MethodZ(int yp);
        }

        public interface V
        {
            void MethodV(int yp);
        }
    }
}
