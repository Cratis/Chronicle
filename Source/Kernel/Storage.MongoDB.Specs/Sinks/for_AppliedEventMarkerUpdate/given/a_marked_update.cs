// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_AppliedEventMarkerUpdate.given;

public class a_marked_update : Specification
{
    protected const string OwnSlot = "__applied.f.0";
    protected const string JoinSlot = "__applied.j.def";

    internal static AppliedEventMarkerGroup Own(UpdateDefinition<BsonDocument>? update, long sequenceNumber = 5) => new(update, new(OwnSlot, "own-stream", sequenceNumber));

    internal static AppliedEventMarkerGroup Join(UpdateDefinition<BsonDocument>? update, BsonValue key, long sequenceNumber = 5, string onProperty = "customerId") => new(update, new(JoinSlot, "join-stream", sequenceNumber), new(onProperty, key));

    protected static UpdateDefinitionBuilder<BsonDocument> Updates => Builders<BsonDocument>.Update;

    internal static BsonDocument Stage(UpdateDefinition<BsonDocument> update)
    {
        var rendered = update.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonArray;
        rendered.Count.ShouldEqual(1);
        return rendered[0].AsBsonDocument["$set"].AsBsonDocument;
    }

    protected static string Json(BsonValue value) => value.ToJson();
}
