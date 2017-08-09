# CodeProxy

[![Build Status](https://travis-ci.org/roberino/CodeProxy.svg?branch=master)](https://travis-ci.org/roberino/CodeProxy)

A dynamic type proxy. Generate type implementations on the fly.

## Installation

Via NuGet - https://preview.nuget.org/packages/CodeProxy/1.0.2-beta

## Example usage

Create an interface:

```cs

public interface IMyInterface
{
   string X { get; set; }
   string SayHi(string message);        
}

```

Add property and method implementations:

```cs

var factory = new ClassFactory<IMyInterface>();
	  
fact.AddPropertyGetter(i => i.X, (p, o, v) => v + "x"); // Create a get method for property "X"

factory.AddMethodImplementation("SayHi", (i, m, p) => {
    // i = interface
    // m = method
    // p = parameters
	var msg = p.Single().Value; // Get the only parameter
    Console.WriteLine(msg);
    return msg;
});

var instance = factory.CreateInstance();

instance.ValueY = "a";

instance.SayHi("say hi");
			
```

See tests for more examples and functionality