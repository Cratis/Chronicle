# Adapters

Adapters defines the relationship between an external model and an internal event sourced
representation of that model. The assertions one typically want to do is to verify that
correct events are being produced for first time imports and any subsequent imports.
In addition it can be very useful to assert the correctness of the projected model, which
then serves as the basis for the comparison within the integration engine.

Read more [here](../clients/dotnet/integration/integration.md), which also holds the
sample artifacts used throughout here.

## Adapter Specification Context

At the core sits a type called `AdapterSpecificationContext<,>`. This does all the heavy lifting
of setting up an in-memory system working with the adapter and all its dependencies.
It provides the **event log** and methods for asserting correctness related to imports.
In addition you can access projected model instances for asserting projected results.

You would only use this directly if you rely on writing a standard xUnit test.

With the types used as examples described [here](../clients/dotnet/integration/integration.md), you
can create the context by doing the following:

```csharp
var adapter = new AccountHolderDetailsAdapter();
var context = new AdapterSpecificationContext<AccountHolder>();
```

In the **context** instance you'll find a property for the `EventLog` and method for getting the instance of
a model by its identifier; `GetById()`. With the `EventLog` you can append events for an identifier (EventSourceId) and
then you can get the expected instance and assert the state.

In the class we define a set of constants:

```chsarp
const string socialSecurityNumber = "12345678901";
const string firstName = "Bør";
const string lastName = "Børson";
static DateTime birthDate = new(1873, 3, 17);
const string address = "Langkaia 1";
const string city = "Oslo";
const string postalCode = "0103";
const string country = "Norge";
```

Then we do the fact assertions:

```csharp
[Fact]
async Task FirstTimeImportShouldAppendExpectedEvents()
{
    // Arrange
    var adapter = new AccountHolderDetailsAdapter();
    var context = new AdapterSpecificationContext<AccountHolder>();
    var objectToImport = new KontoEier(
        socialSecurityNumber,
        firstName,
        lastName,
        birthDate,
        address,
        city,
        postalCode,
        country);

    // Act
    await context.Import(objectToImport);
    result = await context.Projection.GetById(socialSecurityNumber);

    // Assert
    context.ShouldAppendEvents(
        new AccountHolderRegistered(firstName, lastName, birthDate),
        new AccountHolderAddressChanged(address, city, postalCode, country));

    Assert.Equal(result, new AccountHolder(firstName, lastName, birthDate, socialSecurityNumber, address, city, country))
}
```

For a second import without any changes you could then do:

```csharp
[Fact]
async Task SubsequentImportWithoutChangesShouldNotAppendAnyEvents()
{
    // Arrange
    var adapter = new AccountHolderDetailsAdapter();
    var context = new AdapterSpecificationContext<AccountHolder>();
    context.EventLog.Append(socialSecurityNumber, new AccountHolderRegistered(firstName, lastName, birthDate));
    context.EventLog.Append(socialSecurityNumber, new AccountHolderAddressChanged(address, city, postal_code, country));

    var objectToImport = new KontoEier(
        socialSecurityNumber,
        firstName,
        lastName,
        birthDate,
        address,
        city,
        postalCode,
        country);

    // Act
    await context.Import(objectToImport);

    // Assert
    context.ShouldNotAppendEvents();
}
```

## Adapter Specification For

Building on top of the gherkin-ish type of approach in [Aksio Specifications](https://github.com/aksio-insurtech/Specifications),
there is a base class called `AdapterSpecificationFor<>` that sets up and leverages the `AdapterSpecificationContext<>`.
This is exposed in a property called `context`.

The base class is an abstract class requiring you to override a method called `CreateAdapter()` where you return a new instance
of the adapter.

We can typically organize common things in a base `given` context for the specifications, in a namespace with the word `given` in it
for readability:

```csharp
public class object_ready_for_import : AdapterSpecificationFor<AccountHolder, KontoEier>
{
    protected const string social_security_number = "12345678901";
    protected const string first_name = "Bør";
    protected const string last_name = "Børson";
    protected static DateTime birth_date = new(1873, 3, 17);
    protected const string address = "Langkaia 1";
    protected const string city = "Oslo";
    protected const string postal_code = "0103";
    protected const string country = "Norge";

    protected KontoEier object_to_import = new(
        social_security_number,
        first_name,
        last_name,
        birth_date,
        address,
        city,
        postal_code,
        country);

    protected override IAdapterFor<AccountHolder, KontoEier> CreateAdapter() => new AccountHolderDetailsAdapter();
}

```

The specification for first time would then look something like this:

```csharp
public class when_importing_for_the_first_time : given.object_ready_for_import
{
    AccountHolder result;

    async Task Because()
    {
        await context.Import(object_to_import);
        result = await context.Projection.GetById(social_security_number);
    }

    [Fact] void should_append_account_holder_registered() => context.ShouldAppendEvents(new AccountHolderRegistered(first_name, last_name, birth_date));
    [Fact] void should_append_account_holder_address_changed() => context.ShouldAppendEvents(new AccountHolderAddressChanged(address, city, postal_code, country));
    [Fact] void should_project_all_properties() => result.ShouldEqual(new AccountHolder(first_name, last_name, birth_date, social_security_number, address, city, postal_code, country));
}
```

For a second import:

```csharp
public class when_importing_for_the_second_time_without_changes : given.object_ready_for_import
{
    void Establish()
    {
        context.EventLog.Append(social_security_number, new AccountHolderRegistered(first_name, last_name, birth_date));
        context.EventLog.Append(social_security_number, new AccountHolderAddressChanged(address, city, postal_code, country));
    }

    Task Because() => context.Import(object_to_import);

    [Fact] void should_not_append_any_events() => context.ShouldNotAppendEvents();
}
```
