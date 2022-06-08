# Projecting to the outbox

Rather than manually appending to the **outbox**, it is very common that what you want in the
**outbox** is based on state changes done in the **event log**. The easiest thing will then
be to create an outbox projection that projects from the **event log** and puts it in the
**outbox** when there are changes.

With the approach to private events defined [here](./creating-an-event.md) we can imagine two
events that represents the ability to deposit and withdraw money to/from an account.

```csharp
[EventType("adaab3e5-f797-4335-80d4-06758773f7e1")]
public record DepositToDebitAccountPerformed(double Amount);

[EventType("507a71d9-862f-4615-b8e8-2427d9568373")]
public record WithdrawalFromDebitAccountPerformed(double Amount);
````

With a public event that communicates the balance, as defined [here](./creating-a-public-event.md)
We can then drop in an implementation of `IOutboxProjections` from the `Aksio.Cratis.Events.Outbox`
namespace:

```csharp
using Aksio.Cratis.Events.Outbox;

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
