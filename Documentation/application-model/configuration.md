# Configuration

Configuration files needs to reside in a folder called **data** from the working directory of a running solution.
From this you can map files to types by leveraging the `[Configuration("{file}")]` attribute.

Lets say you have a config file called `my-config.json` in the **data** directory looking as the following:

```json
{
    "someString": "Hello world",
    "someInteger": 42
}
```

This can then quite easily be mapped to a type in the following way:

```csharp
using Aksio.Configuration;

[Configuration("my-config")]
public class MyConfig
{
    public string SomeString { get; init; }
    public int SomeInteger { get; init; }
}
```

The system will then create two registrations in the IoC container for getting access to this,
one where you can use the type directly as a dependency and another where you can leverage the
[ASP.NET Core `IOptions<>`](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-5.0).

Below is an example of using the type directly:

```csharp
public class MyController : Controller
{
    public MyController(MyConfig config)
    {
    }
}
```

If you prefer the `IOptions<>` approach, simply wrap it like this:

```csharp
public class MyController : Controller
{
    public MyController(IOptions<MyConfig> config)
    {
    }
}
```

> Note: Effectively these are exactly the same, the `IOptions<>` means accessing the configuration needs to
> go through the `Value` property of the `IOptions<>` interface.

## Search Paths

By default it will search for configuration files in `$PWD/config` and `$PWD`, in that order.
