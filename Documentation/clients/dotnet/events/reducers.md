# Reducers

Reducers are classes that holds methods that react to certain events and produce a read model
as a result. It is intended only for creating read models and not meant as a way to react
and perform tasks, which is what a [reactor](./reactors.md) is for.

By implementing the interface `IReducerFor`, it will automatically be discovered at startup and
the system will figure out which methods will be handling what events by convention.

```csharp
using Cratis.Chronicle.Reactors;

public class MyReducer : IReducerFor<MyReadModel>
{
}
```

The `IReducerFor<>` takes a generic argument, which is the type of the read model the reducer works
with. A read model is basically a `class` or `record` that represents the state events are reduced to.

```csharp
public record MyReadModel(Guid Id, string Something);
```

The methods you add onto the class will be discovered by convention and the system recognizes the following
signatures, name of the method(s) can be anything:

```csharp
public MyReadModel SynchronousMethodWithoutContext(MyEvent @event, MyReadModel? currentInstance);
public MyReadModel SynchronousMethodWithContext(MyEvent @event, MyReadModel? currentInstance, EventContext context);
public Task<MyReadModel> AsynchronousMethodWithoutContext(MyEvent @event, MyReadModel? currentInstance);
public Task<MyReadModel> AsynchronousMethodWithContext(MyEvent @event, MyReadModel? currentInstance, EventContext context);
```

> Note: Only public methods are supported without any return types. Also worth noting is that you can't have
> two methods handling the same event as the system would not know how to recover if one of them fails.

The `currentInstance` argument will hold the current instance from the underlying database.
If there is no instance, this value will be null.

> Note: Deletion is currently not supported, but will be implemented as the following [issue](https://github.com/Cratis/Chronicle/issues/1028) describes.

A concrete example of a reducer:

{{snippet:Quickstart-BooksReducer}}
