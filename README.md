# CodeProxy

[![Build Status](https://travis-ci.org/roberino/CodeProxy.svg?branch=master)](https://travis-ci.org/roberino/CodeProxy)

A dynamic type proxy. Generate type implementations on the fly.

# Example usage

```cs

public interface IMyInterface
{
   string X { get; set; }
   string SayHi(string message);        
}

public class Program {

	public static void Main() {

		var factory = new ClassFactory<IMyInterface>();

		factory.WithPropertyInterceptor((p, v) => v + "x");

		factory.AddMethodImplementation("SayHi", (m, p) => {
			var msg = p.Single().Value; // Get the parameter

            Console.WriteLine(msg);
			
			return msg;
		});

		var instance = factory.CreateInstance();

		instance.ValueY = "a";

		instance.SayHi("say hi");
	}
}
			
```