// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_removing_a_child_from_all;

public class and_the_child_identifier_is_id : Specification
{
    Sink _sink;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    BsonDocument _update;

    void Establish()
    {
        var readModel = new ReadModelDefinition(
            "Orders",
            "orders",
            "Orders",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new JsonSchema() },
            []);
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        var collections = Substitute.For<ISinkCollections>();
        collections.GetCollection().Returns(collection);
        collection.UpdateManyAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _update = call.Arg<UpdateDefinition<BsonDocument>>().Render(new RenderArgs<BsonDocument>(
                    BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(2, 2, null));
            });
        var converter = Substitute.For<IMongoDBConverter>();
        converter.ToBsonValue(Arg.Any<Key>()).Returns(new BsonString("order"));
        var changesetConverter = Substitute.For<IChangesetConverter>();
        changesetConverter.ToUpdateDefinition(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>())
            .Returns(new UpdateDefinitionAndArrayFilters(new BsonDocumentUpdateDefinition<BsonDocument>(new BsonDocument()), [], false));
        _sink = new Sink(readModel, converter, collections, changesetConverter, Substitute.For<IExpandoObjectConverter>(), Substitute.For<IReadModelChangeStreams>());
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([new ChildRemovedFromAll("[items]", "id", "item", ArrayIndexers.NoIndexers)]);
    }

    async Task Because() => await _sink.ApplyChanges(new Key("order", ArrayIndexers.NoIndexers), _changeset, EventSequenceNumber.First);

    [Fact] void should_pull_children_using_the_stored_identifier_name() => _update.ShouldEqual(
        BsonDocument.Parse("""{ "$pull": { "items": { "_id": "item" } } }"""));
}
