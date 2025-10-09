# Constraints in the .NET Client

Constraints in the .NET client are a powerful way to enforce rules and ensure data integrity within the Chronicle event store. By defining constraints, you can specify conditions that must be met for events to be appended to the event store.

## Defining Constraints

To define a constraint, implement the `IConstraint` interface. This interface requires you to define the `Define` method, which takes an `IConstraintBuilder` as a parameter. The `IConstraintBuilder` provides methods to specify the details of the constraint.

### Example Implementation

Here is an example of a custom constraint implementation:

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UniqueEventConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder)
    {
        builder.Unique<MyEventType>(
            message: "Only one instance of MyEventType is allowed per event source."
            // Optionally, you can provide a name for the constraint. If not provided, the system will assign a name based on the type.
        );
    }
}
```

In this example, the `UniqueEventConstraint` ensures that only one instance of `MyEventType` can exist per event source. The `Unique` method of the `IConstraintBuilder` is used to define this rule.

## Unique Constraints with Multiple Events

In some cases, you may want to enforce uniqueness across multiple event types based on a shared property. This can be achieved using the `Unique()` method with `On()` methods to specify the events and properties involved.

### Example: Unique Users

Consider a scenario where you want to ensure that usernames are unique across the following events:

- `UserAdded`
- `UserNameChanged`
- `UserRemoved`

Here is how you can define such a constraint:

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UniqueUserConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder)
    {
        builder.Unique(unique => unique
            .On<UserAdded>(@event => @event.UserName)
            .On<UserNameChanged>(@event => @event.NewUserName)
            .RemovedWith<UserRemoved>()
        );
    }
}
```

### Explanation

- `On<TEvent>(Func<TEvent, object> propertySelector)`: Specifies the event type and the property to enforce uniqueness on.

- `RemovedWith<TEvent>()`: Specifies the event type that removes the uniqueness constraint. For example, when a `UserRemoved` event is processed, the username is no longer considered unique.

This approach creates an index in the Chronicle event store for the specified property, ensuring that the property remains unique across the specified events.

## Automatic Registration

Constraints are automatically picked up and registered by the Chronicle framework. When you implement the `IConstraint` interface and include your constraint in the project, it will be discovered and applied during runtime.

## How Constraints Work

When a constraint is defined, it is registered with the Chronicle event store. The `IConstraintBuilder` allows you to specify various types of constraints, such as uniqueness constraints. These constraints are then enforced by the Chronicle server, ensuring that the defined rules are adhered to.

### Index Creation

When a constraint is registered, Chronicle creates an index in the event store to enforce the constraint. For example, a uniqueness constraint will result in an index that ensures no duplicate values are allowed for the specified field.

## Summary

- Implement the `IConstraint` interface to define custom constraints.
- Use the `IConstraintBuilder` to specify the details of the constraint.
- Constraints are automatically registered and enforced by the Chronicle framework.
- The event store creates indexes to enforce constraints, ensuring data integrity.
- Use `Unique<TEventType>()` for single-instance constraints per event source.
- Use `Unique()` with `On()` methods for property-based uniqueness across multiple events.

By leveraging constraints, you can ensure that your event data adheres to the rules and conditions necessary for your application's integrity.
