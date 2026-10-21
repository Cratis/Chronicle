// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;
using KernelDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;
using StoredDefinition = Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters.given;

public static class legacy_definition_documents
{
    public static BsonDocument For(KernelDefinition definition)
    {
        var document = definition.ToMongoDB().ToBsonDocument();
        document.Remove("storageFormatVersion");
        document.Remove("scope");
        RemoveEveryEventSettings(document);
        return document;
    }

    public static KernelDefinition Restore(BsonDocument document) => BsonSerializer.Deserialize<StoredDefinition>(document).ToKernel();

    static void RemoveEveryEventSettings(BsonDocument document)
    {
        var every = document["fromEvery"].AsBsonDocument;
        every.Remove("key");
        every.Remove("autoMap");
        foreach (var collection in new[] { "children", "nested" })
        {
            foreach (var child in document[collection].AsBsonDocument.Elements)
            {
                RemoveEveryEventSettings(child.Value.AsBsonDocument);
            }
        }
    }
}
