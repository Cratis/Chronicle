# Projections

Projections takes events and project these to a desired model.
The assertions one typically want to do is to verify that they are in fact being projected
to the expected outcome.

## Projection

Lets say you have a read model called `DebitAccount` with a projection called `DebitAccountProjection` that is responsible
for projecting to it from a `DebitAccountOpened` event:

```csharp
[EventType("3daa0bf9-4cca-455e-87bc-c27dade3eb11")]
public record DebitAccountOpened(AccountName Name, PersonId Owner);

public record DebitAccount(AccountId Id, AccountName Name, PersonId Owner, double? Balance);

public class DebitAccountProjection : IProjectionFor<DebitAccount>
{
    public ProjectionId Identifier => "d1bb5522-5512-42ce-938a-d176536bb01d";

    public void Define(IProjectionBuilderFor<DebitAccount> builder) =>
        builder
            .From<DebitAccountOpened>(_ => _
                .Set(model => model.Name).To(@event => @event.Name)
                .Set(model => model.Owner).To(@event => @event.Owner));
}
```

## Projection Specification Context

At the core sits a type called `ProjectionSpecificationContext<>`. This does all the heavy lifting
of setting up an in-memory system for working with projections. It provides the **event log** and
the actual projection that can be worked with. One can then typically append events into the **event log**
and then get an instance of the projection for assertions.

You would only use this directly if you rely on writing a standard xUnit test.

With the projection described earlier, you create this by doing the following:

```csharp
var projection = new DebitAccountProjection();
var context = new ProjectionSpecificationContext<DebitAccount>()
```

In the **context** instance you'll find a property for the `EventLog` and method for getting the instance of
a model by its identifier; `GetById()`. With the `EventLog` you can append events for an identifier (EventSourceId) and
then you can get the expected instance and assert the state.

```csharp
[Fact]
async Task DebitAccountOpenedShouldProjectToExpectedProperties()
{
    const string accountId = "ad6f9665-c77d-410b-8968-19ce9b1784e8";
    const string accountName = "My first account";
    const string ownerId = "f3aed250-3200-487b-8a1a-ce1661ea6fee";

    // Arrange
    var projection = new DebitAccountProjection();
    var context = new ProjectionSpecificationContext<DebitAccount>()

    // Act
    await context.EventLog.Append(accountId, new DebitAccountOpened(accountName, ownerId));
    result = await context.GetById(accountId);

    // Assert
    Assert.Equal(result.Name.Value, accountName);
    Assert.Equal(result.Owner.Value, Guid.Parse(owner_id));
}
```

## Projection Specification For

Building on top of the gherkin-ish type of approach in [Aksio Specifications](https://github.com/aksio-insurtech/Specifications),
there is a base class called `ProjectionSpecificationFor<>` that sets up and leverages the `ProjectionSpecificationContext<>`.
This is exposed in a property called `context`.

The base class is an abstract class requiring you to override a method called `CreateProjection()` where you return a new instance
of the adapter.

The specification would then look something like this for asserting value correctness on the model:

```csharp
public class when_opening_account : ProjectionSpecificationFor<DebitAccount>
{
    const string account_id = "ad6f9665-c77d-410b-8968-19ce9b1784e8";
    const string account_name = "My first account";
    const string owner_id = "f3aed250-3200-487b-8a1a-ce1661ea6fee";

    DebitAccount result;

    protected override IProjectionFor<DebitAccount> CreateProjection() => new DebitAccountProjection();

    async Task Because()
    {
        await context.EventLog.Append(account_id, new DebitAccountOpened(account_name, owner_id));
        result = await context.GetById(account_id);
    }

    [Fact] void should_set_account_name() => result.Name.Value.ShouldEqual(account_name);
    [Fact] void should_set_owner() => result.Owner.Value.ShouldEqual(Guid.Parse(owner_id));
}
```
