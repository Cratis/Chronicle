# Quickstart ASP.NET Core

[!INCLUDE [pre-requisites](./prereq.md)]

## Objective

In this quickstart, you will create a simple solution that covers the most important aspects of getting started with Chronicle
in an ASP.NET Core application.

The sample will focus on a straightforward and well-understood domain: a library.

You can find the complete working sample [here](https://github.com/Cratis/Samples/tree/main/Chronicle/Quickstart/AspNetCore).
which also leverages common things from [here](https://github.com/Cratis/Samples/tree/main/Chronicle/Quickstart/Common).

[!INCLUDE [docker](./docker.md)]

## Setup project

Start by creating a folder for your project and then create a .NET web project inside this folder:

```shell
dotnet new web
```

Add a reference to the [Chronicle package](https://www.nuget.org/packages/Cratis.Chronicle) and
[Chronicle ASP.NET Core package](https://www.nuget.org/packages/Cratis.Chronicle.AspNetCore):

```shell
dotnet add package Cratis.Chronicle.AspNetCore
```

## WebApplication

When using ASP.NET Core you typically use the `WebApplicationBuilder` to build up your application.
This includes having an IOC (Inversion of Control) container setup for dependency injection of all your services.
Chronicle supports this paradigm out of the box, and there are convenience methods for hooking this up real easily.

In your `Program.cs` simply change your setup to the following:

{{snippet:Quickstart-AspNetCore-WebApplicationBuilder}}

The code adds Chronicle to your application and sets the name of the event store to use.
In contrast to what you need to do when running bare-bone as shown in the [console](./console.md) sample,
all discovery and registration of artifacts will happen automatically.

{{snippet:Quickstart-AspNetCore-WebApplication}}

[!INCLUDE [common](./common.md)]

## Using in APIs

Appending events is typically something you would be doing directly, or indirectly through an API exposed as a minimal
API or a Controller.

{{snippet:Quickstart-AspNetCore-BookBorrowed}}

The code exposes an API endpoint that takes parameters for what book to borrow and what user is borrowing it.
It then appends a `BookBorrowed` event. The `bookId` is used as the [event source](../concepts/event-source.md), while
the `userId` is part of the event.

## Services

Chronicle will leverage the IOC container to get instances of any artifacts it discovers and will create instances of,
such as **Reactors**, **Reducers** and **Projections**.
In order for this to work, the artifacts needs to be registered as services. In your `Program.cs` file you would add
service registrations for the artifacts you have:

{{snippet:Quickstart-AspNetCore-ArtifactsServices}}

This can become very tedious to do as your solution grows, Cratis Fundamentals offers a way couple of extension methods
that will automatically hook this up:

```csharp
builder.Services
    .AddBindingsByConvention()
    .AddSelfBindings();
```

The first statement adds service bindings by convention, which is basically any service that implements an interface with the same name only prefixed with `I` will be automatically registered (`IFoo` -> `Foo`). The second statement automatically registers all
class instances as themselves, meaning you can then take dependencies to concrete types without having to register them.

[!INCLUDE [common](./mongodb.md)]

In addition to this, since you're in an environment with dependency injection enabled you typically want to register
the things being used to be able to take these in as dependencies into your types such as controllers, route handler or
any other type that needs it.

For instance, for MongoDB, it can be very convenient to not have to think about the actual `MongoClient` and just focus on
what you need; access to the specific database or specific collections.

The following code shows how to set this up for your `WebApplicationBuilder` to enable that scenario.

{{snippet:Quickstart-AspNetCore-MongoSetup}}

> Note: The code adds service registrations for typed collections based on types found in the [sample code](https://github.com/Cratis/Samples/tree/main/Chronicle/Quickstart/Common).

With this you can now quite easily create a type that encapsulates getting the data that takes a specific collection as a dependency:

{{snippet:Quickstart-Books}}
