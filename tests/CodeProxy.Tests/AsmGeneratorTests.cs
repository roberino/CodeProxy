using NUnit.Framework;
using System;
using System.Linq;

namespace CodeProxy.Tests
{
    [TestFixture]
    public class AsmGeneratorTests
    {
        [Test]
        public void WhenBasicClassDefSupplied_ThenReturnsAssemblyWithOneClass()
        {
            var asmGen = new AsmGenerator();

            var asm = asmGen.Compile("public class X { }");

            Assert.That(asm.ExportedTypes.Single().Name, Is.EqualTo("X"));

            Console.WriteLine(asm.FullName);
        }
    }
}
