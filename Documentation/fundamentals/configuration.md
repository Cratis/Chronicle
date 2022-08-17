# Configuration

With [.NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration) you get a way to create configuration and source it from multiple locations quite easily.
Cratis fundamentals provide a thin wrapper on top of this to provide an approach making it more oriented around
the configuration object type, rather than just the `IOptions<>` pattern. It also provides mechanisms around value resolutions.
All configuration objects gets registered automatically as singleton with the service collection.

To get started, all you need is the [Fundamentals](https://www.nuget.org/packages/Aksio.Cratis.Fundamentals/) package.

## Getting started

Configuration can be defined as C# classes. We do however encourage to make them immutable using [init properties](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init).
The reason for that is that configuration should not be possible to change within code consuming it, it should be changed at the source.

> Note: record types won't work. As the .NET serializers for configuration does not know how to instantiate these with their constructors.

The following is an example of a configuration object:

```csharp
using Aksio.Configuration;

[Configuration]
public class MyConfig
{
    public string SomeString { get; init; } = "Default Value"
    public int SomeInteger { get; init; } = 43;
}
```

> Note: Its a good idea to let properties have a default value, if the value is optional. That way consuming code can rely on the value being set.

Add a JSON file called `myconfig.json` at the root of your project that corresponds to the content:

```json
{
    "someString": "Hello world",
    "someInteger": 42
}
```

The filename it will use for looking for the file is by default the name of the C# type lower cased and with `.json` as file extension.

> Note: If you're using the Cratis Application Model, it will automatically be configured for you and you can therefor skip the configuration part.

The system uses type discovery to automatically register all configuration objects and leverages the [types system](./types.md) found in Fundamentals as well.

If you're using the .NET 6 minimal API surface:

```csharp
using Aksio.Types;

var types = new Types();
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddConfigurationObjects(types);
var app = builder.Build();
```

If you're using the generic [HostBuilder](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-6.0):

```csharp
using Aksio.Types;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var types = new Types();
        await Host.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.AddConfigurationObjects(types))
            .Build();
            .RunAsync();
    }
}
```

The system will then create two registrations in the service collection for using the configuration,
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

## Names

It is possible to override the default name of the configuration. This name will then become what is used as
file name. You do this by using the **name** parameter of the `[Configuration]` attribute.

```csharp
using Aksio.Configuration;

[Configuration("my-config")]
public class MyConfig
{
    public string SomeString { get; init; }
    public int SomeInteger { get; init; }
}
```

The system will then look for a file called `my-config.json` in the one of the search paths looking as the following.

## Base path

You can provide a base relative path for searching. This path will then be combined with the current working directory.

```csharp
services.AddConfigurationObjects(types, baseRelativePath: "config" );
```

This means that that `./config` will now become the base path for all configuration objects.

## Search Paths

By default it will look for configuration files in the current working directory of the running application.
You can provide additional search paths:

```csharp
services.AddConfigurationObjects(types, searchSubPaths: new[] { "config" } );
```

It will then add these as optional search paths in addition to the current working directory and following
the [order of precedence](https://devblogs.microsoft.com/premier-developer/order-of-precedence-when-configuring-asp-net-core/) as defined
by .NET.

> Note: All search paths will honor the base path and be relative to it.

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
            ClusterTypes.AdoNet => new AdoNetClusterOptions(),
            ClusterTypes.AzureStorage => new AzureStorageClusterOptions(),
            _ => null!
        };
    }
}
```

> Important: The resolvers job is just to resolve an instance of the type, not actually bind it with the configuration.
> The configuration system will as the last thing it does, attempt to bind the configuration to the instance recursively.

## Configuration objects within configuration objects

It is possible to create a hierarchy for configuration as well. This can then be expressed with a root **configuration object**
and properties recursively on it or any types within it can then also be adorned with the `[Configuration]` attribute.
This will automatically then hook up all configuration objects into the service collection and can be used as dependencies directly.

An example of this is the [Cratis Kernel configuration object](../../Source/Kernel/Configuration/Shared/KernelConfiguration.cs).

```csharp
[Configuration("cratis")]
public class KernelConfiguration
{
    public Tenants Tenants { get; init; } = new();
    public Cluster Cluster { get; init; } = new();
    public Storage Storage { get; init; } = new();
}
```

All of the properties are **configuration objects** themselves, such as the `Tenants` type:

```csharp
[Configuration]
public class Tenants : Dictionary<string, Tenant>
{
    public IEnumerable<TenantId> GetTenantIds() => Keys.Select(_ => (TenantId)_).ToArray();
}
```

The **tenant** configuration is then available on the root object and can be taken as a dependency directly as well:

```csharp
public class MyController : Controller
{
    public MyController(Tenants tenants)
    {
    }
}
```

## Perform operations after values bound

After the values are bound to a configuration object one might want to be notified to be able to perform operations / preparations
for the object to be used. By implementing `IPerformPostBindOperations` the system will call the `Perform()` method when values have
been bound.

```csharp
[Configuration("cratis")]
public class KernelConfiguration : IPerformPostBindOperations
{
    public Tenants Tenants { get; init; } = new();
    public Cluster Cluster { get; init; } = new();
    public Storage Storage { get; init; } = new();

    public void Perform()
    {
        // Perform post configuration
        Storage.ConfigureKernelMicroservice(Tenants.Select(_ => _.Key));
    }
}
```
