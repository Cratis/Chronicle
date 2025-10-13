# Aggregate Root

The concept of an Aggregate Root comes from [Domain Driven Design](https://martinfowler.com/bliki/DDD_Aggregate.html).
Its role is to govern the interaction of domain objects that should be treated as a single unit.
With event sourcing, an aggregate root typically is responsible for applying events as it sees fit according
to its domain logic and rules.

Said in another way, Aggregate Root objects are responsible for managing the domain transaction and governs the
integrity of the state changes that goes together.

## Overview

In Chronicle, an aggregate root is represented by implementing the `IAggregateRoot` interface or inheriting from the `AggregateRoot` base class. The framework provides two main types of aggregate roots:

1. **Stateless Aggregate Roots** - Simple aggregate roots that don't maintain internal state
2. **Stateful Aggregate Roots** - Aggregate roots that maintain state using reducers, projections, or event handler methods

## Basic Structure

All aggregate roots in Chronicle inherit from the `AggregateRoot` base class:

```csharp
public class MyAggregateRoot : AggregateRoot
{
    // Your domain logic here
}
```

For aggregate roots that need to maintain state, you can use the generic version:

```csharp
public class MyStatefulAggregateRoot : AggregateRoot<MyState>
{
    // Access to state via the protected State property
    public void DoSomething()
    {
        var currentValue = State?.SomeProperty;
        // Domain logic using state
    }
}
```

## Working with Events

### Applying Events

To apply events within an aggregate root, use the `Apply` method:

```csharp
public class UserAggregateRoot : AggregateRoot
{
    public async Task CreateUser(string firstName, string lastName, string email)
    {
        // Validation logic here

        await Apply(new UserCreated
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email
        });
    }
}
```

### Committing Changes

After applying events, you need to commit the changes:

```csharp
public async Task HandleCreateUserCommand(CreateUserCommand command)
{
    var aggregateRoot = await _aggregateRootFactory.Get<UserAggregateRoot>(command.UserId);
    await aggregateRoot.CreateUser(command.FirstName, command.LastName, command.Email);
    await aggregateRoot.Commit();
}
```

## Event Handlers (On Methods)

Chronicle automatically discovers event handler methods in your aggregate root. These methods are called when events are applied or when rehydrating the aggregate from the event store.

### Method Naming Convention

Event handler methods can be named with any prefix you prefer, but commonly use `On` or `Handle`:

```csharp
public class UserAggregateRoot : AggregateRoot
{
    public void OnUserCreated(UserCreated @event)
    {
        // Handle the event
    }

    public Task OnUserNameChanged(UserNameChanged @event, EventContext context)
    {
        // Async handler with event context
        return Task.CompletedTask;
    }
}
```

### Handler Method Signatures

Event handlers can have different signatures:

- `void OnEvent(MyEvent @event)` - Synchronous handler
- `Task OnEvent(MyEvent @event)` - Asynchronous handler
- `void OnEvent(MyEvent @event, EventContext context)` - With event context
- `Task OnEvent(MyEvent @event, EventContext context)` - Async with context

## State Management

Chronicle provides three approaches for managing state in aggregate roots:

### 1. Manual State Management (On Methods)

You can manually manage state by handling events in `On` methods:

```csharp
public class UserAggregateRoot : AggregateRoot
{
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private bool _isActive;

    public void OnUserCreated(UserCreated @event)
    {
        _firstName = @event.FirstName;
        _lastName = @event.LastName;
        _isActive = true;
    }

    public void OnUserDeactivated(UserDeactivated @event)
    {
        _isActive = false;
    }
}
```

### 2. Using Reducers

Create a reducer that builds state from events and use it with a stateful aggregate root:

```csharp
public record UserState(string FirstName, string LastName, bool IsActive);

public class UserReducer : IReducer<UserState>
{
    public UserState? Reduce(UserState? previous, object @event) => @event switch
    {
        UserCreated created => new UserState(created.FirstName, created.LastName, true),
        UserNameChanged nameChanged => previous with { FirstName = nameChanged.FirstName, LastName = nameChanged.LastName },
        UserDeactivated => previous with { IsActive = false },
        _ => previous
    };
}

public class UserAggregateRoot : AggregateRoot<UserState>
{
    public async Task ChangeName(string firstName, string lastName)
    {
        if (State?.IsActive != true)
            throw new InvalidOperationException("Cannot change name of inactive user");

        await Apply(new UserNameChanged
        {
            FirstName = firstName,
            LastName = lastName
        });
    }
}
```

### 3. Using Projections

Use a projection to build read models and access them as state:

```csharp
public class UserProjection : IProjection<UserState>
{
    public void On(UserCreated @event, UserState model, EventContext context)
    {
        model.FirstName = @event.FirstName;
        model.LastName = @event.LastName;
        model.IsActive = true;
    }

    public void On(UserNameChanged @event, UserState model, EventContext context)
    {
        model.FirstName = @event.FirstName;
        model.LastName = @event.LastName;
    }

    public void On(UserDeactivated @event, UserState model, EventContext context)
    {
        model.IsActive = false;
    }
}

public class UserAggregateRoot : AggregateRoot<UserState>
{
    public async Task ChangeName(string firstName, string lastName)
    {
        if (State?.IsActive != true)
            throw new InvalidOperationException("Cannot change name of inactive user");

        await Apply(new UserNameChanged
        {
            FirstName = firstName,
            LastName = lastName
        });
    }
}
```

## Aggregate Root Factory

Use the `IAggregateRootFactory` to create and retrieve aggregate root instances:

```csharp
public class UserService
{
    private readonly IAggregateRootFactory _aggregateRootFactory;

    public UserService(IAggregateRootFactory aggregateRootFactory)
    {
        _aggregateRootFactory = aggregateRootFactory;
    }

    public async Task CreateUser(EventSourceId userId, string firstName, string lastName, string email)
    {
        var userAggregate = await _aggregateRootFactory.Get<UserAggregateRoot>(userId);
        await userAggregate.CreateUser(firstName, lastName, email);
        await userAggregate.Commit();
    }
}
```

## Best Practices

1. **Keep aggregates focused** - Each aggregate should represent a single business concept
2. **Validate within aggregates** - Business rules and validation should be enforced in the aggregate
3. **Emit meaningful events** - Events should represent business events, not technical operations
4. **Use appropriate state management** - Choose between manual state, reducers, or projections based on complexity
5. **Handle invariants** - Use the current state to enforce business rules before applying new events
6. **Keep aggregates small** - Large aggregates can lead to performance and concurrency issues

## Error Handling

Chronicle provides built-in support for handling validation errors and constraint violations during event application and commit operations. The `AggregateRootCommitResult` contains information about any errors that occurred during the commit process.

```csharp
var result = await aggregateRoot.Commit();
if (!result.IsSuccess)
{
    // Handle validation errors or constraint violations
    foreach (var error in result.ValidationResults)
    {
        // Process validation errors
    }
}
```
