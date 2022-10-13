# Serialization

## JsonSerializerOptions

When using the `.AddControllersFromProjectReferencedAssembles()` method, it sets up the default JSON options for serialization for API controllers.
Within this it will set up support for [concepts](./concepts.md) and types such as the `DateOnly`, `TimeOnly` and more converters.
The options are set up for the ASP.NET pipelines as the default options.

If you want to have access to the default `JsonSerializerOptions` in your own code, you can simply take a dependency in your constructor to it:

```csharp
using System.Text.Json;

public class MyService
{
    public MyService(JsonSerializerOptions options)
    {
        // use the options
    }
}
```

Default behavior for the JSON serializer options is also to use **camelCase** instead of **PascalCase** and it will automatically translate between
the two when serializing and deserializing.

> Note: When using the `.UseAksio()` extension in your program, the `.AddControllersFromProjectReferencedAssembles()` will be called.

## Polymorphism and type discriminators

When working with polymorphism and serialization, the serializers don't generally know what the concrete type that it should be serializing from and
deserializing to. It needs something that helps it identify what the actual type is in order to do so.

Different serializers deal with this in different ways. Therefor we have implemented an approach that can be adopted into the different serializers,
without having different approaches to this and having serializer specific metadata.

When serializing to a target format from a type, serializers tend to include type information to be able to deserialize it to the same type.
The problem with this approach is that when persisting this to a database you're code and database has to match and you can't rename or maybe not
even move your code to another namespace. Some serializers offer a way to define a discriminator which could be a string, or a unique identifier
identifying it that does not couple oneself to the name.

In Cratis, we have taken the latter approach, but made it a consistent approach independent of the serializers.

Lets say you have an interface like below:

```csharp
public interface IAccount
{
    AccountId Id { get; }
    AccountName Name { get; }
    AccountType Type { get; }
}
```

To create a concrete implementations of this, all you need is to add a `[DerivedType]` attribute in front of it.
So for our sample, lets say we add a `DebitAccount` and a `CreditAccount`:

```csharp
using Aksio.Cratis.Serialization;

[DerivedType("2c025801-2223-402c-a42a-893845bb1077")]
public record DebitAccount(AccountId Id, AccountName Name, AccountType Type) : IAccount;

[DerivedType("b67b4e5b-d192-404b-ba6f-9647202bd20e")]
public record CreditAccount(AccountId Id, AccountName Name, AccountType Type) : IAccount;
```

The `[DerivedType]` attribute requires a unique identifier in the form of a string representation og a `Guid`.

### JSON

The JSON serializer will add a `_derivedTypeId` to the payload referring to the type discriminator of the type.
With this the consumer can recognize the type.

### MongoDB

There is a specific implementation for MongoDB to be able to serialize and deserialize with the same attributes.
This is automatically hooked up when using the MongoDB extension, implicitly pulled in when using the `.UseAksio()` extension.
Similar to the JSON serializer, this will add a `_derivedTypeId` property to the Bson documents and leverage this on the
way back for deserialization.

## Proxy Generation

The proxy generator supports the derived type mechanism and adds metadata to the typescript files.
This metadata will allow it to properly deserialize known types.

Lets say we create an HTTP GET API endpoint that returns the following C# structure:

```csharp
public record AccountHolderWithAccounts(
    string FirstName,
    string LastName,
    string SocialSecurityNumber,
    Address Address,
    IEnumerable<IAccount> Accounts);
```

This will generate the following:

```typescript
export class AccountHolderWithAccounts {

    @field(String)
    firstName!: string;

    @field(String)
    lastName!: string;

    @field(String)
    socialSecurityNumber!: string;

    @field(Address)
    address!: Address;

    @field(Object, true, [
        CreditAccount,
        DebitAccount
    ])
    accounts!: IAccount[];
}
```

The interface representation will be:

```typescript
export interface IAccount {
    id: string;
    name: string;
    type: AccountType;
}
```

And then for each of the derivatives:

```typescript
@derivedType('2c025801-2223-402c-a42a-893845bb1077')
export class DebitAccount {

    @field(String)
    id!: string;

    @field(String)
    name!: string;

    @field(Number)
    type!: AccountType;
}

@derivedType('b67b4e5b-d192-404b-ba6f-9647202bd20e')
export class CreditAccount {

    @field(String)
    id!: string;

    @field(String)
    name!: string;

    @field(Number)
    type!: AccountType;
}
```

### Client

In the `@aksio/cratis-fundamentals` package you'll find something called `JsonSerializer`.
This is used by the `QueryFor<>` client type. Anything that is coming from the server will go through this serializer
and create a result that holds the correct types.

