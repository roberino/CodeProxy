using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CodeProxy.Tests
{
    [TestFixture]
    public class ClassFactoryTests
    {
        [Test]
        public void WhenPropertySet_ThenPropertyValueCanBeRetrieved()
        {
            var fact = new ClassFactory<X>();

            var instance = fact.CreateInstance();

            instance.ValueY = "a";

            Assert.That(instance.ValueY, Is.EqualTo("a"));
        }

        [Test]
        public void WhenInterceptPropertyGetAndSet_ThenPropertyInterceptorUsed()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertyImplementation((p, v) => v + "x");

            var instance = fact.CreateInstance();

            instance.ValueY = "a";

            Assert.That(instance.ValueY, Is.EqualTo("axx"));
        }

        [Test]
        public void WhenInterceptPropertyWithSetter_ThenSetterInvokedOnGet()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertySetter((p, o, v) => v + "x");

            var instance = fact.CreateInstance();

            instance.ValueY = "a";

            Assert.That(instance.ValueY, Is.EqualTo("ax"));
        }

        [Test]
        public void WhenInterceptPropertyWithGetAndSet_ThenBothInvoked()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertyGetter((p, o, v) => v + "x");
            fact.AddPropertySetter((p, o, v) => v + "y");

            var instance = fact.CreateInstance();

            instance.ValueY = "a";

            Assert.That(instance.ValueY, Is.EqualTo("ayx"));
        }

        [Test]
        public void WhenInterceptPropertyWithSpecificName_ThenNamedPropertySetterInvoked()
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
        public void WhenInterceptPropertyUsingExpression_ThenTargetPropertyImplemented()
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
        public void WhenInterceptPropertyAndClearAndReimplement_ThenNewImplementationUsed()
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
        public async Task WhenInterceptAsyncMethodWithAsyncMethodImplementation_ThenAsyncResultReturned()
        {
            var fact = new ClassFactory<IIsAsync>();

            fact.AddAsyncMethodImplementation(async (i, m, p) =>
            {
                await Task.Delay(2);

                return "hi";
            });

            var instance = fact.CreateInstance();

            var value = await instance.GetStuffAsync();

            Assert.That(value, Is.EqualTo("hi"));
        }

        [Test]
        public void WhenInterceptUnsetProperty_ThenValueReturned()
        {
            var fact = new ClassFactory<X>();

            fact.AddPropertyGetter((p, o, v) => v + "x");

            var instance = fact.CreateInstance();

            Assert.That(instance.ValueX, Is.EqualTo("x"));
            Assert.That(instance.ValueY, Is.EqualTo("x"));
        }

        [Test]
        public void WhenInvokeObjectMethodWithNoInterception_ThenNullReturned()
        {
            var fact = new ClassFactory<Y>();

            var instance = fact.CreateInstance();

            var y = instance.MethodY(12);

            Assert.That(y, Is.Null);
        }

        [Test]
        public void WhenMethodInterceptorWithArg_ThenArgCanBeReferenced()
        {
            var fact = new ClassFactory<Y>();

            fact.AddMethodImplementation((m, p) => p["yp"].ToString());

            var instance = fact.CreateInstance();

            var y = instance.MethodY(12);

            Assert.That(y, Is.EqualTo("12"));
        }

        [Test]
        public void WhenNamedMethodInterceptorAdded_ThenSpecificMethodTargetted()
        {
            var fact = new ClassFactory<Y>();

            fact.AddMethodImplementation("MethodY", (i, m, p) =>
            {
                var val = p.First().Value.ToString();

                return i.ValueY + "/" + val;
            });

            var instance = fact.CreateInstance();

            instance.ValueY = "hi";

            var y = instance.MethodY(12);
            
            Assert.That(y, Is.EqualTo("hi/12"));

            var x = instance.MethodX(123);

            Assert.That(x, Is.Null);
        }

        [Test]
        public void WhenPredicateMethodInterceptorUsed_ThenMatchingMethodIntercepted()
        {
            var fact = new ClassFactory<O>();

            fact.AddMethodImplementation(m => m.GetParameters().First().ParameterType == typeof(int), (i, m, p) =>
            {
                return 3f * Convert.ToSingle(p.First().Value);
            });

            fact.AddMethodImplementation(m => m.GetParameters().First().ParameterType == typeof(string), (i, m, p) =>
            {
                return 4f;
            });

            var instance = fact.CreateInstance();

            var y = instance.MethodO(4);
            var x = instance.MethodO("a");

            Assert.That(y, Is.EqualTo(12));
            Assert.That(x, Is.EqualTo(4));
        }

        [Test]
        public void WhenDifferentPrimativeTypeReturnedByMethodInterceptor_ThenValueConverted()
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
        public void WhenSingleVoidMethodInterceptor_ThenMethodIntercepted()
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

        [Test]
        public async Task WhenAsyncMethodIntercepted_ThenTaskResultReturned()
        {
            var fact = new ClassFactory<IIsAsync>();

            fact.AddMethodImplementation((m, p) =>
            {
                return Task.FromResult("test");
            });

            var instance = fact.CreateInstance();

            var value = await instance.GetStuffAsync();

            Assert.That(value, Is.EqualTo("test"));
        }

        [Test]
        public void WhenInterfaceWithInheritance_ThenInheritedPropertiesCanBeIntercepted()
        {
            var fact = new ClassFactory<IInherit>();

            fact.AddPropertyGetter((i, p, v) => p.Name);

            var instance = fact.CreateInstance();

            Assert.That(instance.ValueX, Is.EqualTo(nameof(instance.ValueX)));
            Assert.That(instance.ValueY, Is.EqualTo(nameof(instance.ValueY)));
        }

        [Test]
        public async Task WhenVirtualClassWithInterface_ThenInheritedPropertiesCanBeIntercepted()
        {
            var fact = new ClassFactory<IsAsync>();

            fact.AddAsyncMethodImplementation(async (i, m, a) => await Task.FromResult("x"));

            var instance = fact.CreateInstance();

            var result = await instance.GetStuffAsync();

            Assert.That(result, Is.EqualTo("x"));
        }

        [Test]
        public async Task WhenAbstractClassWithInterface_ThenInheritedPropertiesCanBeIntercepted()
        {
            var fact = new ClassFactory<IsAbstractAsync>();

            fact.AddAsyncMethodImplementation(async (i, m, a) => await Task.FromResult("x"));

            var instance = fact.CreateInstance();

            var result = await instance.GetStuffAsync();

            Assert.That(result, Is.EqualTo("x"));
        }

        [Test]
        public void WhenGenericInterface_CanCreateInstance()
        {
            var fact = new ClassFactory<IGeneric<string>>();

            var instance = fact.CreateInstance();

            Assert.That(instance, Is.Not.Null);
        }

        public class IsAsync : IIsAsync
        {
            public virtual Task<string> GetStuffAsync()
            {
                return Task.FromResult("hey");
            }
        }

        public interface IGeneric<T> 
        {
            T GetStuff();
        }

        public abstract class IsAbstractAsync : IIsAsync
        {
            public abstract Task<string> GetStuffAsync();
        }

        public interface IIsAsync
        {
            Task<string> GetStuffAsync();
        }

        public interface IInherit : X
        {
            string GetStuff();
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
            string MethodX(int yp);
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
