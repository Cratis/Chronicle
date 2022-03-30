# Concepts

To express a domain in a better way rather than representing input parameters, fields or properties as primitives such as `int`, `string` or `Guid`,
we formalize these as their own types. This increases readability in the code and also represents an opportunity for doing cross cutting concerns.
The type itself will then represent metadata that can be leveraged at runtime to reason about the code and also provide functionality such as
authorization or validation rules based on the type.

When we do these formalization, there is an underlying base type that represents these called `ConceptAs<>`. With it we can handle cross cutting
things like serialization of these types.

The wrapped type is not necessarily what you want to have represented when storing in databases or during transport. There are therefor
implementations for the common things we use to translate the concepts to its underlying type.

## Creating a concept

Lets say we have in our domain an entity called `Person`. It is identified by its social security number:

```csharp
public record Person(string SocialSecurityNumber)
```

Instead of using `string`, we want this to actually be a specific type of `SocialSecurityNumber`:

```csharp
public record SocialSecurityNumber(string value) : ConceptAs<string>(value);
```

Using it will then be:

```csharp
public record Person(SocialSecurityNumber SocialSecurityNumber)
```

When we use this new type our APIs become much clearer based on its signature:.

```csharp
public interface IPersons
{
    Person GetBy(SocialSecurityNumber socialSecurityNumber);
}
```

## Implicit operators

The `ConceptAs<>` base record has an implicit operator overload for converting from the formalized type to the underlying primitive type.
Due to a limitation of C#, the underlying `ConceptAs<>` does not have an implicit operator overload going the other way.
It might not be needed either, but if you typically have the value represented by its primitive type and want a convenient way to automatically
convert it to the formalized type; you can easily add an implicit operator for this:

```csharp
public record SocialSecurityNumber(string value) : ConceptAs<string>(value)
{
    public static implicit operator SocialSecurityNumber(string value) => new(value);
}
```

With this you get full interchangeability:

```csharp
    string socialSecurityNumber = "12345678901";
    SocialSecurityNumber formalized = socialSecurityNumber;
    string unwrapped = formalized;
```

## System.Text.Json

Within the **Fundamentals** package you'll find a namespace called `Json`. This holds converters for serializing and deserializing concept types.

With the default behavior of the `JsonSerializer`:

```csharp
using System.Text.Json;

var person = new Person("12345678901"); // Implicit operator converting it to SocialSecurityNumber
var serialized = JsonSerializer.Serialize(person);
```

The output of would be:

```json
{
    "SocialSecurityNumber": {
        "Value": "12345678901"
    }
}
```

By leveraging the converter:

```csharp
using System.Text.Json;
using Aksio.Cratis.Json;

var options = new JsonSerializerOptions
{
    Converters =
    {
        new ConceptAsJsonConverterFactory()
    }
}

var person = new Person("12345678901"); // Implicit operator converting it to SocialSecurityNumber
var serialized = JsonSerializer.Serialize(person, options);
```

The output will be:

```json
{
    "SocialSecurityNumber": "12345678901"
}
```

The converter handles deserializing it correctly. Just pass in the converter factory:

```csharp
using System.Text.Json;
using Aksio.Cratis.Json;

var options = new JsonSerializerOptions
{
    Converters =
    {
        new ConceptAsJsonConverterFactory()
    }
}

var json = "{ \"SocialSecurityNumber\": \"12345678901\" }";
var person = JsonSerializer.Deserialize<Person>(json, options);
```

> Note: If you're using the Cratis Application Model, you do not have to configure this. It is automatically configured for the ASP.NET pipelines
> and other parts that needs it, such as the Cratis Kernel transports.

## TypeConverters

The .NET component model has the concept of a [type converter](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.typeconverter?view=net-6.0).
These are leveraged by different parts of the .NET platform, including ASP.NET when dealing with things like parameters for controllers.
Out of the box the **Fundamentals** provide a type converter for concepts. All you need to do is register it for your concept types.

```csharp
using System.ComponentModel;

TypeDescriptor.AddAttributes(typeof(SocialSecurityNumber), new TypeConverterAttribute(typeof(ConceptAsTypeConverter<SocialSecurityNumber>)));
```

This can become a lot of registrations and a cognitive load of having to remember to this.
One way to make it simpler would be to discover all `ConceptAs<>` implementations and register them automatically.
With the [type](./types.md) system in the **Fundamentals** you can easily do this. There is however a convenience extension method for
the `ITypes` type that does this for you.

```csharp
using Aksio.Cratis.Concepts;
using Aksio.Cratis.Types;

var types = new Types();
types.RegisterTypeConvertersForConcepts();
```

> Note: If you're using the Cratis Application Model, you do not have to manually set this up. It is automatically configured at startup.

## MongoDB

As with Json serialization, when we store things in MongoDB we want it to be stored in a way that makes more sense for readability but also
for us to be able to do queries on them.

In the [MongoDB extension](https://www.nuget.org/packages/Aksio.Cratis.Extensions.MongoDB/) package there is a MongoDB serializer for dealing with
this in the same way.

You can either use the serializer manually on maps defined or you can register it globally as shown below:

```csharp
using Aksio.Cratis.Extensions.MongoDB;
using MongoDB.Bson.Serialization;

BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());
```

> Note: If you're using the Cratis Application Model, you do not have to manually set this up. It is automatically configured at startup.
