using NUnit.Framework;
using System;
using System.Linq;

namespace CodeProxy.Tests
{
    [TestFixture]
    public class AsmGeneratorTests
    {
        [Test]
        public void Compile_EmptyClass_ReturnsAssemblyWithOneClass()
        {
            var asmGen = new AsmGenerator();

            var asm = asmGen.Compile("public class X { }");

            Assert.That(asm, Is.Not.Null);
            Assert.That(asm.ExportedTypes.Count(), Is.EqualTo(1));

            Console.WriteLine(asm.FullName);
        }
    }
}
