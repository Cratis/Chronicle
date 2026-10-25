// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sinks.for_Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_GenerationWriterGrants;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_publication_revokes_factory_writers(ReplicaSetMongoDBFixture fixture) : Specification
{
    readonly Key _key = new("one", ArrayIndexers.NoIndexers);
    MongoMaterializationBindingStorageHarness _harness;
    IMaterializationBindingStorage _bindings;
    IGenerationBuildStorage _builds;
    IGenerationWriterGrants _grants;
    SinkFactory _factory;
    ServiceProvider _services;
    GenerationSinkContext _first;
    GenerationSinkContext _second;
    ISink _active;
    ISink _candidate;
    ISink _fresh;
    MaterializationPublication _publication;
    Exception? _oldActiveError;
    Exception? _recreatedError;
    BulkFlushResult _lateFlush;

    async Task Establish()
    {
        _harness = new() { ConnectionString = fixture.ConnectionString };
        _bindings = _harness.Create();
        _builds = (IGenerationBuildStorage)_bindings;
        _grants = (IGenerationWriterGrants)_bindings;
        _first = GenerationContexts.Create(WellKnownSinkTypes.MongoDB, schemaGeneration: 7);
        await _bindings.TryAdoptFirst("store", _first.Binding.Key, new(_first.Binding.Identity, MaterializationGenerationState.Active, MaterializationRevision.Unknown, DateTimeOffset.UnixEpoch));
        await _bindings.TrySetBindingEpoch("store", _first.Binding.Key, ServingEpoch.Unset, ServingEpoch.Initial);
        await _builds.TryAdoptExecution("store", _first.Binding.Key, _first.Execution);
        _first = _first with { Grant = (await _grants.TryAcquire("store", _first.Binding, new(Guid.NewGuid()), 0))! };
        var reserved = await _builds.ReserveNext("store", _first.Binding.Key, _first.Execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch);
        var binding = (await _builds.ResolveObserver("store", "namespace", _first.Binding.EventSequence, reserved.Record!.Identity.ObserverId))!;
        _second = new(binding, _first.Execution, (await _grants.TryAcquire("store", binding, new(Guid.NewGuid()), 0))!);
        ConfigureFactory();
        _active = _factory.CreateFor("store", "namespace", _first);
        _candidate = _factory.CreateFor("store", "namespace", _second);
        await Task.WhenAll(_active.EnsureIndexes(), _candidate.EnsureIndexes());
        await _active.ReplaceIsolated(_key, State("active"), 1UL);
        await _candidate.ReplaceIsolated(_key, State("candidate"), 1UL);
        await _candidate.BeginBulk();
        await _candidate.ApplyChanges(_key, ChangingName(), 2UL);
        await _bindings.TryMarkFollowing("store", _first.Binding.Key, _second.Binding.Identity.Generation);
    }

    void ConfigureFactory()
    {
        var database = Substitute.For<IDatabase>();
        database.GetReadModelDatabase("store", "namespace").Returns(_harness.Database("store", "namespace"));
        var formats = new TypeFormats();
        _services = new ServiceCollection().AddLogging().AddSingleton(database)
            .AddSingleton<IExpandoObjectConverter>(new ExpandoObjectConverter(formats)).AddSingleton(Substitute.For<IReadModelChangeStreams>())
            .AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions())).BuildServiceProvider();
        _factory = new(_services, formats);
    }

    async Task Because()
    {
        _publication = await _bindings.TryPublish("store", _first.Binding.Key, MaterializationGeneration.First, ServingEpoch.Initial, _second.Binding.Identity.Generation, TimeSpan.FromHours(1), new(Guid.NewGuid()));
        _oldActiveError = await Catch.Exception(() => _active.ReplaceIsolated(_key, State("stale"), 3UL));
        _lateFlush = await _candidate.FlushBulk();
        var recreated = _factory.CreateFor("store", "namespace", _second);
        _recreatedError = await Catch.Exception(() => recreated.ReplaceIsolated(_key, State("stale"), 3UL));
        var published = (await _bindings.Get("store", _first.Binding.Key))!;
        var grant = (await _grants.TryAcquire("store", _second.Binding, new(Guid.NewGuid()), published.Fence!.Epoch))!;
        _fresh = _factory.CreateFor("store", "namespace", _second with { Grant = grant });
        await _fresh.EnsureIndexes();
        await _fresh.ReplaceIsolated(_key, State("fresh"), 3UL);
    }

    static ExpandoObject State(string name)
    {
        dynamic state = new ExpandoObject();
        state.name = name;
        return state;
    }

    static IChangeset<AppendedEvent, ExpandoObject> ChangingName()
    {
        var changes = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changes.InitialState.Returns(State("candidate"));
        changes.Changes.Returns([new PropertiesChanged<ExpandoObject>(State("late"), [new PropertyDifference("name", "candidate", "late")])]);
        return changes;
    }

    [Fact] void should_publish_in_the_same_database_as_the_sink_gate() => _publication.Result.ShouldEqual(MaterializationPublicationResult.Published);
    [Fact] void should_refuse_the_retired_active_writer() => _oldActiveError.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_report_the_late_flush_without_advancing_progress() => _lateFlush.FailedPartitions.ShouldNotBeEmpty();
    [Fact] void should_not_claim_a_new_epoch_when_recreating_a_stale_candidate() => _recreatedError.ShouldBeOfExactType<FenceRevoked>();
    [Fact] async Task should_only_write_the_new_explicitly_granted_value() => ((IDictionary<string, object?>)(await _fresh.FindOrDefault(_key))!)["name"].ShouldEqual("fresh");
    [Fact] async Task should_leave_the_retired_collection_unchanged() => ((IDictionary<string, object?>)(await _active.FindOrDefault(_key))!)["name"].ShouldEqual("active");
    [Fact] async Task should_not_drop_indexes_on_grant_refresh() => (await (await _harness.Database("store", "namespace").GetCollection<BsonDocument>(_second.Binding.Identity.Destination.Value).Indexes.ListAsync()).ToListAsync()).Count.ShouldEqual(2);

    void Destroy()
    {
        (_active as IDisposable)?.Dispose();
        (_candidate as IDisposable)?.Dispose();
        (_fresh as IDisposable)?.Dispose();
        _services?.Dispose();
        _harness?.Dispose();
    }
}
