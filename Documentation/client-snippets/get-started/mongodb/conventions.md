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
            // We want to ignore extra elements that might be in the documents, Chronicle adds some metadata to the documents
            new IgnoreExtraElementsConvention(true),

            // Chronicle uses camelCase for element names, so we need to use this convention
            new CamelCaseElementNameConvention()
        };
        ConventionRegistry.Register("conventions", pack, t => true);
    }
}
```
