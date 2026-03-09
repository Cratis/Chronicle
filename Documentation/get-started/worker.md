# Quickstart Worker Service

[!INCLUDE [pre-requisites](./prereq.md)]

## Objective

In this quickstart, you will set up Chronicle in a .NET worker service (generic host) application — without ASP.NET Core.
Worker services are ideal for background processing: reacting to events, running scheduled jobs, or maintaining derived data in message-processing pipelines.

[!INCLUDE [docker](./docker.md)]

## Setup project

Start by creating a folder for your project and then create a .NET worker service project inside this folder:

```shell
dotnet new worker
```

Add a reference to the [Chronicle client package](https://www.nuget.org/packages/Cratis.Chronicle):

```shell
dotnet add package Cratis.Chronicle
```

> **Note:** For worker services you only need the base `Cratis.Chronicle` package — the `Cratis.Chronicle.AspNetCore` package is for web applications only.

## Host setup

Open your `Program.cs` and configure Chronicle using `AddCratisChronicle` on the `IHostApplicationBuilder`:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddCratisChronicle(options =>
{
    options.EventStore = "MyWorkerApp";
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
```

The `AddCratisChronicle` call:

- Registers `IChronicleClient`, `IEventStore`, and all the event store components (`IEventLog`, `IReactors`, `IReducers`, `IProjections`, `IReadModels`) in the DI container.
- Automatically discovers and registers all artifacts (Reactors, Reducers, Projections) from the loaded assemblies.
- Reads configuration from the `Cratis:Chronicle` section of `appsettings.json` (connection string, timeouts, etc.).

## Configuration

Chronicle reads its connection settings from `appsettings.json`. Add the following to yours:

```json
{
  "Cratis": {
    "Chronicle": {
      "ConnectionString": "chronicle://localhost:35000",
      "EventStore": "MyWorkerApp"
    }
  }
}
```

You can also configure the event store name inline (as shown above) and keep the connection string in configuration.

## Worker implementation

Inject `IEventStore` or any of the event store sub-services into your hosted service:

```csharp
public class Worker(IEventStore eventStore) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Connect to Chronicle and start processing
        await eventStore.Connection.Connect();

        // Keep running until the host shuts down
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
```

## Structural dependencies

For custom identity providers, correlation ID accessors, or namespace resolvers, use the `configure` callback:

```csharp
builder.AddCratisChronicle(
    configureOptions: options => options.EventStore = "MyWorkerApp",
    configure: b => b
        .WithIdentityProvider(new MyServiceIdentityProvider())
        .WithNamespaceResolver(new MyTenantResolver()));
```

See [Structural Dependencies](../configuration/structural-dependencies.md) for a full list of configurable dependencies.

## Namespace resolution

By default the worker uses the default namespace for all operations. To support multi-tenant scenarios, provide a custom `IEventStoreNamespaceResolver` via `ChronicleClientOptions`:

```csharp
builder.AddCratisChronicle(options =>
{
    options.EventStore = "MyWorkerApp";
    options.EventStoreNamespaceResolverType = typeof(MyTenantNamespaceResolver);
});
```

Or pass a resolver instance directly through the builder:

```csharp
builder.AddCratisChronicle(
    configureOptions: options => options.EventStore = "MyWorkerApp",
    configure: b => b.WithNamespaceResolver(new MyTenantNamespaceResolver(config)));
```

See [Namespace resolution](../namespaces/dotnet-client.md) for details on built-in resolvers.

## Services

Chronicle uses the DI container to create instances of Reactors, Reducers, and Projections it discovers. Register them as services in `Program.cs`:

```csharp
builder.Services.AddTransient<MyReactor>();
builder.Services.AddTransient<MyReducer>();
```

For larger solutions, the Cratis Fundamentals convention-based registration helpers keep this manageable:

```csharp
builder.Services
    .AddBindingsByConvention()
    .AddSelfBindings();
```
