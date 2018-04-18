using System;
using System.Linq;
using gherk = global::Gherkin;

namespace CodeProxy.Gherkin
{
    public class Generator
    {
		//https://github.com/cucumber/gherkin-dotnet/tree/master/Gherkin/Ast
        public Generator()
        {
            var parser = new gherk.Parser();

            var doc = parser.Parse("");

            var arg = doc.Feature.Children.First().Steps.First().Argument;
        }
    }
}
