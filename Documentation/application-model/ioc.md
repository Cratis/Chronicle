# IoC - Inversion of Control

One of the core mindsets throughout the solution is the concept of IoC - inversion of control.
Not only is the **Application Model** built with this in mind, but also for you as a consumer of
the **Application Model**.

## Why

The concept of IoC stems from the [dependency inversion principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle).
In short; this means that you specify your external dependencies as constructor parameters rather than taking on the
responsibility of the creation of it in your code. In addition to this, everything is configured with an
IoC container - [Inversion of Control container](https://en.wikipedia.org/wiki/Inversion_of_control).
It takes on the role of being able to provide these dependencies and also be responsible for the lifecycle
management of these. This gives a lot of flexibility and makes it easier to create decoupled and maintainable
software.

## Design by contract

The dependencies should be represented as contracts, or interfaces. The concrete implementation should not be
important for the consumer of a dependency. It should just care about the exposed API and this API should be
represented as an interface. You can read more about the concept [here](https://en.wikipedia.org/wiki/Design_by_contract).

An added benefit of this is it becomes a lot easier to provide automated tests (TDD), or specs (BDD) that confirms
the behavior of the system. Every unit is then in isolation and you don't need to worry about integrating with
infrastructure in order for you to set up reliable running contexts for your tests or specs. With the interface / contract,
you can simply provide a [test double](https://duckduckgo.com/?q=mock+fake+stub&t=osx).

### Autofac

For the backend C# code, [Autofac](https://autofac.org) has been setup and ready to be leveraged. Autofac provides
more capabilities than the built-in lightweight IoC container of ASP.NET Core.

### Autofac Module

One of the things you can do with Autofac is write [modules](https://autofac.readthedocs.io/en/latest/configuration/modules.html)
that take on the responsibility of registering bindings. Any implementation of such a module is automatically discovered
and hooked up on startup. The benefit of this is that you can keep your binding registrations close to where they belong, making
your codebase more cohesive.

All you need to do is drop in a `Module` implementation anywhere and override the `Load` method, like below:

```csharp
using Autofac;

public class CarTransportModule : Module
{
  public bool ObeySpeedLimit { get; set; }

  protected override void Load(ContainerBuilder builder)
  {
    builder.RegisterType<Car>().As<IVehicle>();

    if (ObeySpeedLimit)
    {
      builder.RegisterType<SaneDriver>().As<IDriver>();
    }
    else
    {
      builder.RegisterType<CrazyDriver>().As<IDriver>();
    }
  }
}
```

## Lifetime

One of the benefits of using an IoC container is that we can control the lifetime of object instances from a higher level.
This could be done using configuration or through automatic conventions. Lifetime can be specified during binding as
detailed [here](https://autofac.readthedocs.io/en/latest/lifetime/index.html).

### Singleton

Most of the time you'll find that for lifetimes such as singleton (one instance per process), you have this knowledge
in the implementation and you want to specify it in the implementation rather than during registration.
Within the fundamentals you'll find an attribute called `[Singleton]`.
This will in combination with conventions make sure that the implementation is registered with singleton lifetime.

```csharp
using Aksio.Cratis.Execution;

[Singleton]
public class MySystem : IMySystem
{
}
```

### Singleton per Tenant

Similar to `[Singleton]` there is another attribute called `[SingletonPerTenant]`. This makes sure to hold one instance
per [tenant](../tenancy.md).

```csharp
using Aksio.Cratis.Execution;

[SingletonPerTenant]
public class MySystem : IMySystem
{
}
```

## Conventions

If something ends up being a repeatable list of things to remember when doing something, its a good chance it could
be automated by a computer. Conventions are about those recognized patterns and hooking them up. You can easily go
and create your own [Autofac module](https://autofac.readthedocs.io/en/latest/configuration/modules.html) providing
an automatic registration representing your conventions in your project.

Out of the box there is one convention setup; a default convention of matching an interface with an implementation
if the name of the interface is the same as the implementation but prefixed with **I** (IFoo -> Foo).

```csharp
public interface IMySystem
{
}

public class MySystem : IMySystem
{
}
```

This is a very common pattern and is automatically registered with the IoC. The convention honors the lifetime
attributes.
