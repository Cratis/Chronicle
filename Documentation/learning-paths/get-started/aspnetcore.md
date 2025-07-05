# ASP.NET Core

Start by creating a folder for your project and then create a .NET web project inside this folder:

```shell
dotnet new web
```

Add a reference to the [Chronicle ASP.NET Core package](https://www.nuget.org/packages/Cratis.Chronicle.AspNetCore):

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
