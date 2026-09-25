```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

public static class GetStartedMongoDbDefaults
{
    public static void Configure()
    {
        BsonSerializer
            .RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        var pack = new ConventionPack
        {
            // Chronicle adds its own metadata fields to every document, such as __subject.
            new IgnoreExtraElementsConvention(true)

            // Element names match your C# property names by default. Add a
            // CamelCaseElementNameConvention here only if you configured Chronicle
            // with the camel case naming policy.
        };
        ConventionRegistry.Register("conventions", pack, t => true);
    }
}
```
