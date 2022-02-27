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

## Configuration Value Resolver

Configuration objects that has complex values that vary by type, one can implement a resolver that resolves
the instance.

The following configuration type has a property that is of type `object` and is a dynamic type based on another
value in the configuration (`Type`).

```csharp
using Aksio.Configuration;

[Configuration]
public class Cluster
{
    public string Name { get; init; } = "Cratis";
    public string Type { get; init; } = ClusterTypes.Local;
    public string AdvertisedIP { get; init; } = "127.0.0.1";
    public int SiloPort { get; init; } = 11111;
    public int GatewayPort { get; init; } = 30000;
    [ConfigurationValueResolver(typeof(ClusterOptionsValueResolver))]   // Add this to tell the configuration binder how to resolve the instance
    public object Options { get; init; } = null!;
}
```

The actual resolver will then look for the value of `type` from the config and resolve the object accordingly.

```csharp
using Aksio.Configuration;
using Microsoft.Extensions.Configuration;
using Orleans.Clustering.AzureStorage;
using Orleans.Configuration;

public class ClusterOptionsValueResolver : IConfigurationValueResolver
{
    /// <inheritdoc/>
    public object Resolve(IConfiguration configuration)
    {
        return configuration.GetValue<string>("type") switch
        {
            ClusterTypes.Static => new StaticClusterOptions(),
            ClusterTypes.AdoNet => new AdoNetClusteringSiloOptions(),
            ClusterTypes.AzureStorage => new AzureStorageClusteringOptions(),
            _ => null!
        };
    }
}
```

> Important: The resolvers job is just to resolve an instance of the type, not actually bind it with the configuration.
> The configuration system will as the last thing it does, attempt to bind the configuration to the instance recursively.

