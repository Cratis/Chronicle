// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.given;

public class a_changeset_converter_recording_markers : a_changeset_converter
{
    protected const string EventTypeId = "event-type";
    protected const string SourceValue = "source-1";
    protected const string Namespace = "ns";

    protected Key _key = new("root-key", ArrayIndexers.NoIndexers);
    protected BsonDocument _joinFilter;
    protected BsonDocument _joinUpdate;
    protected int _joinWrites;

    protected static string Stream => Cratis.Chronicle.Storage.Sinks.AppliedEventMarkers.StreamHash(Namespace, SourceValue);

    internal AppliedEventMarkerContext Context(bool recordOwnSlot = true) => new(Namespace, SourceValue, EventTypeId, 42UL, recordOwnSlot);

    protected static PropertiesChanged<ExpandoObject> Direct() =>
        new(new ExpandoObject(), [new PropertyDifference("name", "Before", "After")]);

    protected static Joined JoinOn(string onProperty) =>
        new(new ExpandoObject(), "joined-source", new PropertyPath(onProperty), ArrayIndexers.NoIndexers, [Direct()]);

    protected static BsonDocument Render(FilterDefinition<BsonDocument> filter) =>
        filter.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));

    internal Task<UpdateDefinitionAndArrayFilters> Convert(IEnumerable<Change> changes, AppliedEventMarkerContext? markers) =>
        ((IChangesetConverterWithDirectWrites)_converter).ToUpdateDefinition(
            _key,
            changes,
            null,
            42UL,
            new SinkMultiDocumentWrites(_sinkCollections, UnfencedSinkWrites.Instance),
            markers);

    protected static BsonDocument RenderUpdate(UpdateDefinition<BsonDocument> update) =>
        update.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;

    void Establish()
    {
        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(call => new MongoDBProperty(call.Arg<PropertyPath>().ToString(), []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(call => BsonValue.Create(call.ArgAt<object>(0)));
        _mongoDBConverter.ToBsonValue((EventSequenceNumber)42UL).Returns(new BsonInt64(42));
        _collection.UpdateManyAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _joinWrites++;
                _joinFilter = Render(call.Arg<FilterDefinition<BsonDocument>>());
                _joinUpdate = RenderUpdate(call.Arg<UpdateDefinition<BsonDocument>>());
                return new UpdateResult.Acknowledged(2, 2, null);
            });
    }
}
