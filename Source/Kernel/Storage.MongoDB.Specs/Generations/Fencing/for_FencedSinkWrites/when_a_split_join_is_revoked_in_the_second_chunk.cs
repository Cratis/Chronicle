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
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedSinkWrites;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_split_join_is_revoked_in_the_second_chunk(ReplicaSetMongoDBFixture fixture) : given(fixture)
{
    Exception? _error;
    long _changed;
    long _unchanged;

    async Task Because()
    {
        await _models.InsertManyAsync(Enumerable.Range(1, 5).Select(id => new BsonDocument { { "_id", id }, { "joinKey", "common" }, { "name", "Before" } }));
        var probe = Substitute.For<IFencedBatchProbe>();
        var attempts = 0;
        probe.BeforeAdmission(Arg.Any<FenceBatchId>()).Returns(async _ =>
        {
            // Attempt one aborts at the cap; attempt two commits the first split chunk.
            if (++attempts == 3) await Revoke();
        });
        using var writes = new FencedSinkWrites(new FixedFenceTokenSource(_token), new FencedBatchExecutor(_store, _policy, probe), 2);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"integer"},"joinKey":{"type":"string"},"name":{"type":"string"}}}
            """);
        var readModel = new ReadModelDefinition("joined-model", "JoinedModel", "models", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        var expando = new ExpandoObjectConverter(new TypeFormats());
        var converter = new MongoDBConverter(expando, new TypeFormats(), readModel, NullLogger<MongoDBConverter>.Instance);
        var collections = Substitute.For<ISinkCollections>();
        collections.GetCollection().Returns(_models);
        var changes = new ChangesetConverter(readModel, converter, collections, expando, NullLogger<ChangesetConverter>.Instance, writes);
        var joined = new Joined(new ExpandoObject(), "common", "joinKey", ArrayIndexers.NoIndexers, [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", "Before", "After")])]);
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([joined]);
        _error = await Catch.Exception(() => writes.InBatch(() => changes.ToUpdateDefinition(new Key("common", ArrayIndexers.NoIndexers), changeset, 42UL)));
        _changed = await _models.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("name", "After"));
        _unchanged = await _models.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("name", "Before"));
    }

    [Fact] void should_fail_the_whole_operation() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_keep_the_first_committed_chunk() => _changed.ShouldEqual(2L);
    [Fact] void should_not_write_the_second_chunk() => _unchanged.ShouldEqual(3L);
}
