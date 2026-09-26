// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Net;
using System.Reflection;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_applying_a_dotted_update_to_a_legacy_null_parent : Specification
{
    Sink _sink;
    IMongoCollection<BsonDocument> _collection;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    int _calls;
    UpdateDefinition<BsonDocument> _original;

    void Establish()
    {
        var definition = new ReadModelDefinition(
            "probe",
            "Probe",
            "Probe",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        var converter = Substitute.For<IMongoDBConverter>();
        converter.ToBsonValue(Arg.Any<Key>()).Returns(BsonString.Create("probe-1"));
        var collections = Substitute.For<ISinkCollections>();
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collections.GetCollection().Returns(_collection);
        var changesetConverter = Substitute.For<IChangesetConverter>();
        _original = Builders<BsonDocument>.Update.Set("outer.info.name", "ciphertext");
        changesetConverter.ToUpdateDefinition(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>())
            .Returns(new UpdateDefinitionAndArrayFilters(_original, [], true));
        _sink = new Sink(definition, converter, collections, changesetConverter, Substitute.For<IExpandoObjectConverter>());
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(),
            [new PropertyDifference(new PropertyPath("outer.info.name"), null, "Again")])]);
        _collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _calls++;
                if (_calls == 1)
                {
                    var writeError = (WriteError)typeof(WriteError)
                        .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(constructor => constructor.GetParameters().Length == 4)
                        .Invoke([ServerErrorCategory.Uncategorized, 28, "Cannot create field", new BsonDocument()]);
                    return Task.FromException<UpdateResult>(new MongoWriteException(new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))), writeError, null!, null!));
                }
                return Task.FromResult<UpdateResult>(null!);
            });
    }

    async Task Because() => await _sink.ApplyChanges(new Key("probe-1", ArrayIndexers.NoIndexers), _changeset, EventSequenceNumber.Unavailable);

    [Fact] void should_repair_both_null_ancestors_and_retry_once() => _calls.ShouldEqual(4);
    [Fact] void should_retry_the_original_update() => _collection.Received(2).UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), _original, Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
}
