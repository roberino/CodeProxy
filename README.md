# CodeProxy

Dynamic type proxy

# Example usage

```cs

var factory = new ClassFactory<IMyInterface>();

factory.WithPropertyInterceptor((p, v) => v + "x");

factory.AddMethodImplementation((m, p) => p["yp"].ToString();

var instance = factory.CreateInstance();

instance.ValueY = "a";

instance.MethodA("say hi");
			
```