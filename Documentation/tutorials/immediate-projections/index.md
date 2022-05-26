# Immediate projections

Unlike regular projections an immediate projection does not save its result for reuse, all is handled in-memory.
This is very helpful for those scenarios where you need a special purpose model and specific instance based on
an identifier.

Following the [Bank sample](../../../Samples/Bank/) we can imagine a part of the system that looks at suspicious
activity that could resemble money laundering. For the simplicity of the sample the logic will basically be
looking for an unusual amount of debit accounts being opened.

In the sample we will refer to this type of observers as *Reactions*. Its purpose is to react to certain conditions.
For structural purposes, it is recommended that you keep these in a separate C# project called **Reactions**.

Within this new project, since this is related to **Accounts**, add a folder called **Accounts**.
Then create an observer that will react to the `DebitAccountOpened`. Create a C# file called `MoneyLaundering`
and add the following to it:

```csharp
namespace Reactions.Accounts;

[Observer("6f71990b-8a96-4bf0-92ad-76f31cd7819f")]
public class MoneyLaundering
{
    public async Task AccountOpened(DebitAccountOpened @event, EventContext context)
    {
        await Task.CompletedTask;
    }
}
```

Since we are going to look for the number of occurrences of the `DebitAccountOpened` event we will need
a model to keep the count and a projection that defines the projection.

Add a file called `AccountsCounter` and add the following:

```csharp
namespace Reactions.Accounts;

public record AccountsCounter(int Count);
```

Then add a file for the projection called `AccountsCounterProjection`:

```csharp
using Events.Accounts.Debit;

namespace Reactions.Accounts;

public class AccountsCounterProjection : IImmediateProjectionFor<AccountsCounter>
{
    public ProjectionId Identifier => "14e8d5b0-9476-4059-a5e2-09439a98a890";

    public void Define(IProjectionBuilderFor<AccountsCounter> builder) => builder
        .From<DebitAccountOpened>(_ => _.Count(m => m.Count));
}
```

We are now ready to start using this projection in our **Reaction**, we do this by adding a constructor
that takes a dependency to `IImmediateProjections`.

```csharp
[Observer("6f71990b-8a96-4bf0-92ad-76f31cd7819f")]
public class MoneyLaundering
{
    readonly IImmediateProjections _immediateProjections;

    public MoneyLaundering(IImmediateProjections immediateProjections)
    {
        _immediateProjections = immediateProjections;
    }

    public async Task AccountOpened(DebitAccountOpened @event, EventContext context)
    {
        await Task.CompletedTask;
    }
}
```

With this in place we can now get an instance of the model by the event source identifier we have in the `EventContext`:

```csharp
[Observer("6f71990b-8a96-4bf0-92ad-76f31cd7819f")]
public class MoneyLaundering
{
    readonly IImmediateProjections _immediateProjections;

    public MoneyLaundering(IImmediateProjections immediateProjections)
    {
        _immediateProjections = immediateProjections;
    }

    public async Task AccountOpened(DebitAccountOpened @event, EventContext context)
    {
        // Get the instance
        var count = await _immediateProjections.GetInstanceById<AccountsCounter>(context.EventSourceId);
        if (count.Count > 42)
        {
            // Perform an action as the reaction
        }
    }
}
```

> Note: The **immediate projections** system will automatically find the projection as there is a one-to-one relation
> between the model type and its projection. This means that if you reuse the same model for two different projections,
> the system will throw an exception.

## React by appending another event

Instead of performing a specific task, we could for the interest of auditing and also decoupling of the system actually
just append another event specific for the purpose.

In the events project, add a file called `PossibleMoneyLaunderingDetected` and add the following to it:

```csharp
using Concepts;

namespace Events.Accounts.Debit;

[EventType("66740d58-cf08-4f30-b793-6d9a306a9eef")]
public record PossibleMoneyLaunderingDetected(PersonId PersonId, EventSourceId AccountId);
```

In the `MoneyLaundering` we can now take the `IEventLog` as a dependency and change our **reaction**:

```csharp
[Observer("6f71990b-8a96-4bf0-92ad-76f31cd7819f")]
public class MoneyLaundering
{
    readonly IImmediateProjections _immediateProjections;
    readonly IEventLog _eventLog;

    public MoneyLaundering(IImmediateProjections immediateProjections, IEventLog eventLog)
    {
        _immediateProjections = immediateProjections;
        _eventLog = eventLog;
    }

    public async Task AccountOpened(DebitAccountOpened @event, EventContext context)
    {
        var count = await _immediateProjections.GetInstanceById<AccountsCounter>(context.EventSourceId);
        if (count.Count > 42)
        {
            // Append for a global event source identifier. This new event can then be used in a projection and other observers that deal
            // with the possible detection.
            await _eventLog.Append(Guid.Empty.ToString(), new PossibleMoneyLaunderingDetected(@event.Owner, context.EventSourceId));
        }
    }
}
```
