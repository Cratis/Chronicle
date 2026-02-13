## MongoDB

When leveraging the Reducer and Projection capabilities of Chronicle, your MongoDB Client needs to be configured
to match how it produces documents and naming conventions. By adding the following code, you'll have something that
matches:

```csharp
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
```

[Snippet source](https://github.com/cratis/samples/blob/main/Chronicle/Quickstart/Common/MongoDBDefaults.cs#L16-L27)
