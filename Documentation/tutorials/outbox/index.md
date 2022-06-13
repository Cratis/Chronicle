# Outbox

Cratis enables autonomous microservices:

* Microservices can run without having to connect directly to another microservice.
* Every microservice has its copy of data it needs to be able to run.

For a reliable system, this makes sense as their are no dependencies that can be broken
or unfulfilled and cause problems for each microservice. It also adds flexibility to your
system by enabling one to take down microservices either temporarily or permanently without
them having a consequence to any of the other microservices.

The way Cratis enables this is by providing a separate event sequence called the **Outbox**.
Only events marked as public can go into the **Outbox**. This is to make a clear distinction from
what you have privately, they represent the public contract to the outside world.

> Note: Versioning (up/down-casting) of events will give a way to version these between systems.
> Enabling the producing microservices and the consuming microservices to be at different
> versions of the event types. This is a planned and upcoming feature of Cratis.

## Public Event

The outbox only supports appending types that are meant for public consumption. They are marked
as public to differentiate from the private events to the microservice.

Following the [Bank sample](../../../Samples/Banking/Bank/) we can imagine a part of the system that enables
publishes the balance of an account as it changes.

Start by creating a file in the **Events.Public** project (./Events.Public/Accounts/Debit) called `AccountBalance.cs`.
Add the following to it:

```csharp
namespace Events.Public.Accounts.Debit;

[EventType("cf0a9242-706e-4293-bd3d-e795b9348bd6", isPublic: true)]
public record AccountBalance(double Balance);
```

## Projecting

With the declarative projection engine in Cratis, we can define something that will automatically
project the balance based on private events.

Add a file in the **Public** project (./Public/Accounts/Debit) called `DebitAccountsOutboxProjections.cs`.
The following projection will react to deposits and withdrawals from the account and generate an object
with the correct balance:

```csharp
namespace Public.Accounts.Debit;

public class DebitAccountsOutboxProjections : IOutboxProjections
{
    public ProjectionId Identifier => "e53ae165-1658-486c-b03c-7c6041428851";

    public void Define(IOutboxProjectionsBuilder builder) => builder
        .For<AccountBalance>(p => p
            .From<DepositToDebitAccountPerformed>(_ => _
                .Add(m => m.Balance).With(e => e.Amount))
            .From<WithdrawalFromDebitAccountPerformed>(_ => _
                .Subtract(m => m.Balance).With(e => e.Amount)));
}
```

Go to the [inbox tutorial](../inbox/index.md) to understand how to consume this in another microservice.
