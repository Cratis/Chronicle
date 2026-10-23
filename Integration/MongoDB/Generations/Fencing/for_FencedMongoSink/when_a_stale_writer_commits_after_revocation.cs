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
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;

namespace Cratis.Chronicle.MongoDB.Integration.Generations.Fencing.for_FencedMongoSink;

[Collection(MongoDBCollection.Name)]
public class when_a_stale_writer_commits_after_revocation(ChronicleInProcessFixture fixture) : given.a_mongo_client(fixture)
{
    readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly string _databaseName = $"fenced_sink_{Guid.NewGuid():N}";
    Exception? _error;
    Exception? _admittedError;
    BsonDocument _persisted;

    async Task Because()
    {
        var database = _client.GetDatabase(_databaseName);
        await database.CreateCollectionAsync("models");
        var gates = new FenceGates(database);
        var token = await gates.Claim("integration/models", 1);
        var probe = Substitute.For<IFencedBatchProbe>();
        var pause = false;
        probe.BeforeCommit(Arg.Any<FenceBatchId>()).Returns(async _ =>
        {
            if (!pause)
            {
                return;
            }
            _reached.TrySetResult();
            await _release.Task.WaitAsync(TimeSpan.FromSeconds(30));
        });
        using var writes = new FencedSinkWrites(new FixedFenceTokenSource(token), new FencedBatchExecutor(new MongoFenceStore(database), FenceRetryPolicy.Default, probe));
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"count":{"type":"integer"}}}
            """);
        var readModel = new ReadModelDefinition("model", "models", "Model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var collections = new SinkCollections(readModel, database, null, writes);
        var converter = new MongoDBConverter(expando, formats, readModel, Microsoft.Extensions.Logging.Abstractions.NullLogger<MongoDBConverter>.Instance);
        var changes = new ChangesetConverter(readModel, converter, collections, expando, Microsoft.Extensions.Logging.Abstractions.NullLogger<ChangesetConverter>.Instance, writes);
        var sink = new Sink(readModel, converter, collections, changes, expando, new ReadModelChangeStreams(Microsoft.Extensions.Logging.Abstractions.NullLogger<ReadModelChangeStreams>.Instance), Microsoft.Extensions.Logging.Abstractions.NullLogger<Sink>.Instance, writes);
        var key = new Key("one", ArrayIndexers.NoIndexers);
        await sink.ApplyChanges(key, SettingCountTo(1), 1UL);
        pause = true;
        var pending = Catch.Exception(() => sink.ApplyChanges(key, SettingCountTo(2), 2UL));
        Task? revocation = null;
        try
        {
            await _reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
            revocation = gates.Revoke(token.MaterializationKey, token.Generation);
        }
        finally
        {
            _release.TrySetResult();
        }
        _admittedError = await pending;
        await revocation;
        pause = false;
        _error = await Catch.Exception(() => sink.ApplyChanges(key, SettingCountTo(3), 3UL));
        _persisted = await database.GetCollection<BsonDocument>(readModel.ContainerName).Find(Builders<BsonDocument>.Filter.Eq("_id", "one")).SingleAsync();
    }

    [Fact] public void should_reject_the_stale_commit() => Assert.IsType<FenceRevoked>(_error);
    [Fact] public void should_commit_the_admitted_transaction_before_revocation_completes() => Assert.Null(_admittedError);
    [Fact] public void should_preserve_the_pre_revocation_commit() => Assert.Equal(2, _persisted["count"].AsInt32);

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);

    static IChangeset<AppendedEvent, ExpandoObject> SettingCountTo(int count)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = count;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference("count", null, count)])]);
        return changeset;
    }
}
