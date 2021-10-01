# Types

Within the fundamentals of the foundation you'll find a package called `Cratis.Types`.
This package provides mechanisms for discovering types in the project referenced assemblies
you have in your project. It will look at the entry assembly and find all its project references
at runtime and index the types from all of these.

The type discovery system is meant to make it easier to automate tasks at runtime, for instance
remove the need for configuration of types to include in a system and discover them based on your
criteria instead.

If you want to bypass any automatic hookup of the system, you can manually create an instance of
the class called `Types` in the `Cratis.Types` namespace. This implements the interface `ITypes`.

## Assembly prefixes

The constructor for `Types` supports taking an 'opt-in' filter for including assemblies in type discovery.
This will make it possible to include more assemblies in addition to the default project referenced
assemblies only. The strings you pass to it are considered prefixes, meaning that if you want to include
a set of assemblies all starting with the same string, you simply put the common start.

```csharp
using Cratis.Types;

var types = new Types("Microsoft","SomeOther");
```

## Type Discovery

There are basically 2 ways of discovering types:

* Using the APIs found in `ITypes` where you can easily get access to all discovered types or find types based on common base types / interfaces.
* Use the `IImplementationsOf<>` as a dependency and get all implementations of a specific type using generic parameters.

```csharp
using Cratis.Types;

public class MySystem
{
    public MySystem(ITypes types)
    {
        // Find multiple implementors of a specific interface...
        types.FindMultiple<ISomeInterface>();

        // ... or using its Type
        types.FindMultiple(typeof(ISomeInterfaec));
    }
}
```

An optimization of this would be the `IImplementationsOf<>`:

```csharp
using Cratis.Types;

public class MySystem
{
    public MySystem(IImplementationsOf<ISomeInterface> someInterfaceTypes)
    {
        // Loop through someInterfaceTypes and do stuff
    }
}
```

> Note: The `ITypes` interface also has an `All` property where you can basically filter types based on your own custom criteria.

## As instances

A common scenario is to discover types were the implementation has dependencies themselves and instances would need to be resolved using
the IoC container. The `IImplementationsOf<>` interface provides this mechanism in a convenient way.

```csharp
using Cratis.Types;

public class MySystem
{
    public MySystem(IInstancesOf<ISomeInterface> someInterfaceTypes)
    {
        // Loop through someInterfaceTypes and do stuff
    }
}
```

> Note: The instances are only created when looping through. The instances are not cached and if you enumerate it multiple times, it will ask the IoC again for the instance.
