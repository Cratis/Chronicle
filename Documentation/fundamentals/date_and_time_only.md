# Date & Time Only

In .NET 6 there were two types introduced called `DateOnly` and `TimeOnly`.
These are unfortunately not fully supported in serializers and typically type conversion for use with
Web APIs. Cratis offers serializers and type converters for this which are automatically hooked up
in the application model.

## System.Text.Json converters

Within the **Fundamentals** package you'll find a namespace called `Json`. This holds converters for serializing and deserializing both `DateOnly` and `TimeOnly` types.

```csharp
using System.Text.Json;
using Aksio.Cratis.Json;

var options = new JsonSerializerOptions
{
    Converters =
    {
        new DateOnlyJsonConverter(),
        new TimeOnlyJsonConverter()
    }
};

var account = new Account
{
    RegisteredDate = new DateOnly(2022, 8, 19),
    RegisteredTime = new TimeOnly(13, 37, 0);
}
var serialized = JsonSerializer.Serialize(person, options);
```

The output of would be:

```json
{
    "registeredDate": "2022-08-19",
    "registeredTime": "13:37:00.1200720"
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
};

var json = "{ \"registeredDate\": \"2022-08-19\", \"registeredTime\": \"13:37:00.1200720\" }";
var person = JsonSerializer.Deserialize<Person>(json, options);
```

> Note: If you're using the Cratis Application Model, you do not have to configure this. It is automatically configured for the ASP.NET pipelines
> and other parts that needs it, such as the Cratis Kernel transports.

## Type Converters


The .NET component model has the concept of a [type converter](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.typeconverter?view=net-6.0).
These are leveraged by different parts of the .NET platform, including ASP.NET when dealing with things like parameters for controllers.
Out of the box the **Fundamentals** provide a type converter for concepts. All you need to do is register it for the `DateOnly`and `TimeOnly` types.

```csharp
using System.ComponentModel;
using Aksio.Cratis.Conversion;

TypeDescriptor.AddAttributes(typeof(DateOnly), new TypeConverterAttribute(typeof(DateOnlyTypeConverter)));
TypeDescriptor.AddAttributes(typeof(TimeOnly), new TypeConverterAttribute(typeof(TimeOnlyTypeConverter)));
```

There is a convenience method for registering these and other converters in **Fundamentals**:

```csharp
using Aksio.Cratis.Conversion;

TypeConverters.Register();
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

BsonSerializer.RegisterSerializationProvider(new DateOnlySerializer());
BsonSerializer.RegisterSerializationProvider(new TimeOnlySerializer());
```

> Note: If you're using the Cratis Application Model, you do not have to manually set this up. It is automatically configured at startup.
