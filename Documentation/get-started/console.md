# Quickstart Console

[!INCLUDE [pre-requisites](./prereq.md)]

## Objective

In this quickstart, you will create a simple solution that covers the most important aspects of getting started with Chronicle.
The sample will focus on a straightforward and well-understood domain: a library.

You can find the complete working sample [see documentation](https://github.com/Cratis/Samples/tree/main/Chronicle/Quickstart/Console).
which also leverages common things from [see documentation](https://github.com/Cratis/Samples/tree/main/Chronicle/Quickstart/Common).

[!INCLUDE [docker](./docker.md)]

## Setup project

Start by creating a folder for your project and then create a .NET console project inside this folder:

```shell
dotnet new console
```

Add a reference to the [Chronicle client package](https://www.nuget.org/packages/Cratis.Chronicle):

```shell
dotnet add package Cratis.Chronicle
```

## Client

Chronicle is accessed through its client called `ChronicleClient`.
From this instance, you can get the event store you want to work with.

The simplest approach is to rely on the automatic discovery of artifacts by instructing the event store to
discover and register everything automatically.

The following snippet configures the minimum and discovers everything for you:

```csharp
using Cratis.Chronicle;

// Explicitly use the Chronicle Options to set the naming policy to camelCase for the projection/reducer sinks
using var client = new ChronicleClient(ChronicleOptions.FromUrl("http://localhost:35000").WithCamelCaseNamingPolicy());
var eventStore = await client.GetEventStore("Quickstart");
```

[Snippet source](https://github.com/cratis/samples/blob/main/Chronicle/Quickstart/Console/Program.cs#L11-L15)

> **Note:** By default, Chronicle will discover and register all client artifacts (Reactors, Reducers, Projections, etc.) with the Chronicle server.
> However, the artifact types themselves are **not automatically registered** in any dependency injection container.
> In a barebones console application without a Host or DI container, only Reactors and Reducers with **parameterless constructors**
> (no dependencies) will work. If your Reactors or Reducers require dependencies, you will need to set up a Host with dependency
> injection or manually provide instances through a custom `IServiceProvider`.

[!INCLUDE [common](./common.md)]

[!INCLUDE [common](./mongodb.md)]

With this you can query the collections as expected using the **MongoDB.Driver**:

```csharp
public class Books(IMongoCollection<Book> collection)
{
    public IEnumerable<Book> GetAll() => collection.Find(Builders<Book>.Filter.Empty).ToList();
}
```

[Snippet source](https://github.com/cratis/samples/blob/main/Chronicle/Quickstart/Common/Books.cs#L9-L12)
