using NUnit.Framework;
using System.Linq;

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
        public void InterceptProperty_GetAndSet()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertyImplementation((p, v) => v + "x");

            var instance = fact.CreateInstance();

            instance.ValueY = "a";

            Assert.That(instance.ValueY, Is.EqualTo("axx"));
        }

        [Test]
        public void InterceptProperty_GetOnly()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertySetter((p, o, v) => v + "x");

            var instance = fact.CreateInstance();

            instance.ValueY = "a";

            Assert.That(instance.ValueY, Is.EqualTo("ax"));
        }

        [Test]
        public void InterceptProperty_GetThenSet()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertyGetter((p, o, v) => v + "x");
            fact.AddPropertySetter((p, o, v) => v + "y");

            var instance = fact.CreateInstance();

            instance.ValueY = "a";

            Assert.That(instance.ValueY, Is.EqualTo("ayx"));
        }

        [Test]
        public void InterceptProperty_GetThenSetNamed()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertyGetter("ValueX", (p, o, v) => v + "x");
            fact.AddPropertyGetter("ValueY", (p, o, v) => v + "y");
            fact.AddPropertySetter("ValueX", (p, o, v) => "xb" + v);
            fact.AddPropertySetter("ValueY", (p, o, v) => "yb" + v);

            var instance = fact.CreateInstance();

            instance.ValueY = "-";
            instance.ValueX = "-";

            Assert.That(instance.ValueX, Is.EqualTo("xb-x"));
            Assert.That(instance.ValueY, Is.EqualTo("yb-y"));
        }

        [Test]
        public void InterceptProperty_UsingExpression()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertyGetter(x => x.ValueX, (p, o, v) => v + "x");
            fact.AddPropertyGetter(x => x.ValueY, (p, o, v) => v + "y");
            fact.AddPropertySetter(x => x.ValueX, (p, o, v) => "xb" + v);
            fact.AddPropertySetter(x => x.ValueY, (p, o, v) => "yb" + v);

            var instance = fact.CreateInstance();

            instance.ValueY = "-";
            instance.ValueX = "-";

            Assert.That(instance.ValueX, Is.EqualTo("xb-x"));
            Assert.That(instance.ValueY, Is.EqualTo("yb-y"));
        }

        [Test]
        public void InterceptProperty_ClearAndReimplement()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertyGetter(x => x.ValueX, (p, o, v) => v + "x");

            var instance = fact.CreateInstance();
            
            instance.ValueX = "y";

            Assert.That(instance.ValueX, Is.EqualTo("yx"));

            fact.ClearAllPropertyImplementations();
            
            fact.AddPropertyGetter(x => x.ValueX, (p, o, v) => v + "z");

            Assert.That(instance.ValueX, Is.EqualTo("yz"));
        }

        [Test]
        public void Create_SimpleInterface_Intercept_Null()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertyGetter((p, o, v) => v + "x");

            var instance = fact.CreateInstance();

            Assert.That(instance.ValueX, Is.EqualTo("x"));
            Assert.That(instance.ValueY, Is.EqualTo("x"));
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

            fact.AddMethodImplementation((m, p) => p["yp"].ToString());

            var instance = fact.CreateInstance();

            var y = instance.MethodY(12);

            Assert.That(y, Is.EqualTo("12"));
        }

        [Test]
        public void Create_InterfaceWithMethods_NamedMethodInterceptor()
        {
            var fact = new ClassFactory<Y>();

            fact.AddMethodImplementation("MethodY", (i, m, p) =>
            {
                var val = p.Single().Value.ToString();

                return i.ValueY + "/" + val;
            });

            var instance = fact.CreateInstance();

            instance.ValueY = "hi";

            var y = instance.MethodY(12);

            Assert.That(y, Is.EqualTo("hi/12"));
        }

        [Test]
        public void Create_InterfaceWithMethods_PredicateMethodInterceptor()
        {
            var fact = new ClassFactory<O>();

            fact.AddMethodImplementation(m => m.GetParameters().First().ParameterType == typeof(int), (i, m, p) =>
            {
                return 3f;
            });

            var instance = fact.CreateInstance();

            var y = instance.MethodO(12);

            Assert.That(y, Is.EqualTo(3));
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
            string ValueX { get; set; }
        }

        public interface Y
        {
            string ValueY { get; set; }
            string MethodY(int yp);
        }

        public interface Z
        {
            float MethodZ(int yp);
        }

        public interface O
        {
            float MethodO(int yp);
            float MethodO(string yp);
        }

        public interface V
        {
            void MethodV(int yp);
        }
    }
}
