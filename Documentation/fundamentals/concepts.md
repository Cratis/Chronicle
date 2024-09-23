# Concepts

To express a domain in a better way rather than representing input parameters, fields or properties as primitives such as `int`, `string` or `Guid`,
we formalize these as their own types. This increases readability in the code and also represents an opportunity for doing cross cutting concerns.
The type itself will then represent metadata that can be leveraged at runtime to reason about the code and also provide functionality such as
authorization or validation rules based on the type.

The encapsulation should only be for a single value. That means you shouldn't have more properties than a single value. This is due to how all
the serializers work internally, they wouldn't know how to resolve anything but a single value. All serializers and converters converts back and
forth from the primitive representation.

When we do these formalization, there is an underlying base type that represents these called `ConceptAs<>`. With it we can handle cross cutting
things like serialization of these types.

`ConceptAs<>` implements `IComparable` and makes it possible to compare the primitive types without having to unwrap the concept. It is
important to note that it inherently means that the type the concept represents needs to be a type that implements `IComparable`, which is
true for primitives.

The wrapped type is not necessarily what you want to have represented when storing in databases or during transport. There are therefor
implementations for the common things we use to translate the concepts to its underlying type.

## Enums

Enums are on their own already a domain concept and does not need further encapsulation.

## Serialization

Concepts using `ConceptAs<>` is meant as encapsulations of primitives, single values and aims to be a way of getting type safety and compile-time checking.
The encapsulation is however very inconvenient when used in data transport or saved in a data store, as it would by default become a container with
a single property `Value` within it. To overcome this, there are serializers and converters provided that unwraps the inner value and also then
knows how to create instances of a concept when getting it back into the code. These assume that the value is a single value, so if you were to
add more values these would be completely ignored and lost.

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

> Note: It is important to note that anything that implements `ConceptAs<>` is assumed to be a wrapper for a primitive type only.
> All serialization converters assume this and will fail if one puts more properties on it.
> If one needs to represent complex types, you can do so without inheriting from `ConceptAs<>` and then have all its values be concepts instead.

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

## Nullability

While the formalized domain concept is a representation, or a wrapper of another type. The nullability of the underlying type is not important.
The `ConceptAs<>` constructor does not allow for the value to be null. The reason for this is that you would be camouflaging whether or not
null is an allowed value for the concept.

Imagine:

```csharp
public record Person(SocialSecurityNumber SocialSecurityNumber)
```

If null is valid and the `ConceptAs<>` didn't have this constraint, you would end up with an instance for `SocialSecurityNumber` with a null value within it.
This would hide the intent and possibly create problems down the line. While a more clearer approach, if null is allowed, to make it nullable and very clear.

```csharp
public record Person(SocialSecurityNumber? SocialSecurityNumber)
```

You can now see clearly that this value can be null. If this is a common theme for the concept, you can make the implicit operator handle nullables as well.

```csharp
public record SocialSecurityNumber(string value) : ConceptAs<string>(value)
{
    public static implicit operator SocialSecurityNumber?(string value) => value == default ? default : new(value);
}
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
using Cratis.Json;

var options = new JsonSerializerOptions
{
    Converters =
    {
        new ConceptAsJsonConverterFactory()
    }
};

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
using Cratis.Json;

var options = new JsonSerializerOptions
{
    Converters =
    {
        new ConceptAsJsonConverterFactory()
    }
};

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
using Cratis.Concepts;
using Cratis.Types;

var types = new Types();
types.RegisterTypeConvertersForConcepts();
```

> Note: If you're using the Cratis Application Model, you do not have to manually set this up. It is automatically configured at startup.
