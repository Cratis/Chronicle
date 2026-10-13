// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Traces;
using MongoDB.Bson;
using MongoDB.Driver;

using context = Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers.and_live_state_replays_through_the_shared_kernel.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers;

[Collection(ChronicleCollection.Name)]
public class and_live_state_replays_through_the_shared_kernel(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        readonly Ledger _ledger = new();
        readonly EventSourceId _source = EventSourceId.New();
        public SelfState Live;
        public SelfState Replayed;
        public ClassicState ClassicLive;
        public ClassicState ClassicReplayed;
        public string[] SelfHooks;
        public string[] ClassicHooks;
        public bool SelfHistoryEmpty;
        public bool ClassicHistoryEmpty;
        public BsonDocument? Stored;
        public override IEnumerable<Type> EventTypes => [typeof(Added), typeof(Deleted)];
        public override IEnumerable<Type> Reducers => [typeof(SelfState), typeof(Classic)];

        async Task Because()
        {
            // MutableServiceRegistry does not track factory-created hook arguments for disposal.
            // A separate native store and real provider give BOTH forms the same DI lifetime semantics.
            var services = new ServiceCollection().AddSingleton(_ledger).AddScoped<Work<SelfState>>().AddScoped<Work<Classic>>().AddTransient<Classic>();
            var sequenceType = typeof(ChronicleClient).Assembly.GetType("Cratis.Chronicle.EventSequences.EventSequence", throwOnError: true)!;
            foreach (var owner in new[] { sequenceType, typeof(Reactors.Reactors), typeof(Reducers.Reducers) })
            {
                var activityType = typeof(IActivitySource<>).MakeGenericType(owner);
                services.AddKeyedSingleton(activityType, ClientActivity.SourceName, Services.GetRequiredKeyedService(activityType, ClientActivity.SourceName));
            }
            await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
            using var client = new ChronicleClient(
                new BorrowedConnection(EventStore.Connection),
                new ChronicleOptions { DefaultSinkTypeId = ChronicleClient.Options.DefaultSinkTypeId },
                artifactsProvider: this,
                serviceProvider: provider);
            var store = await client.GetEventStore(EventStore.Name, $"hooks-{Guid.NewGuid():N}");
            await store.RegisterAll();
            var handlers = new[] { store.Reducers.GetHandlerFor(typeof(SelfState)), store.Reducers.GetHandlerFor<Classic>() };
            foreach (var handler in handlers) await handler.WaitTillSubscribed();
            (await (await store.EventLog.Append(_source, new Added(9))).WaitForCompletion()).IsSuccess.ShouldBeTrue();
            (await (await store.EventLog.Append(_source, new Deleted())).WaitForCompletion()).IsSuccess.ShouldBeTrue();
            (await store.ReadModels.GetInstanceById<SelfState>(_source)).ShouldBeNull();
            (await store.ReadModels.GetInstanceById<ClassicState>(_source)).ShouldBeNull();
            (await (await store.EventLog.Append(_source, new Added(2))).WaitForCompletion()).IsSuccess.ShouldBeTrue();
            (await (await store.EventLog.Append(_source, new Added(3))).WaitForCompletion()).IsSuccess.ShouldBeTrue();
            Live = await store.ReadModels.GetInstanceById<SelfState>(_source);
            ClassicLive = await store.ReadModels.GetInstanceById<ClassicState>(_source);
            if (ChronicleFixture.Options.StorageProvider == ChronicleStorageProvider.MongoDB)
            {
                using var mongo = new MongoClient($"mongodb://{ChronicleFixture.MongoDBContainer.Hostname}:{ChronicleFixture.MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true");
                var databaseName = DatabaseNames.ForReadModels(new Concepts.EventStoreName(store.Name.Value), new Concepts.EventStoreNamespaceName(store.Namespace.Value), DatabaseNamePrefix);
                var collection = mongo.GetDatabase(databaseName).GetCollection<BsonDocument>(handlers[0].Invoker.ContainerName.Value);
                Stored = await collection.Find(new BsonDocument("_id", _source.Value)).SingleAsync();
            }
            SelfHistoryEmpty = !(await store.ReadModels.GetSnapshotsById<SelfState>(_source)).Any();
            ClassicHistoryEmpty = !(await store.ReadModels.GetSnapshotsById<ClassicState>(_source)).Any();
            foreach (var handler in handlers)
            {
                var job = await store.Reducers.Replay(handler.Id);
                job.ShouldNotEqual(JobId.NotSet);
                await store.Jobs.WaitTillJobCompletesOrIsDeleted(job);
                await handler.WaitTillActive();
                (await handler.GetFailedPartitions()).ShouldBeEmpty();
            }
            Replayed = await store.ReadModels.GetInstanceById<SelfState>(_source);
            ClassicReplayed = await store.ReadModels.GetInstanceById<ClassicState>(_source);
            SelfHooks = _ledger.For(typeof(SelfState)).ToArray();
            ClassicHooks = _ledger.For(typeof(Classic)).ToArray();
            client.EvictEventStores();
        }
    }

    [Fact] void should_create_and_recreate_constructor_only_state() => Context.Live.Total.ShouldEqual(5);
    [Fact] void should_preserve_the_ordered_immutable_collection() => Context.Live.Values.ShouldEqual<int>([2, 3]);
    [Fact] void should_match_classic_live_state() => Context.Live.Total.ShouldEqual(Context.ClassicLive.Total);
    [Fact] void should_rebuild_the_same_state_on_full_replay() => Context.Replayed.Total.ShouldEqual(5);
    [Fact] void should_match_classic_replayed_collections() => Context.Replayed.Values.ShouldEqual<int>(Context.ClassicReplayed.Values.ToArray());
    [Fact] void should_preserve_classic_full_replay_notifications() => Context.ClassicHooks.Where(action => action != "disposed").ShouldEqual(["begin", "end"]);
    [Fact] void should_share_classic_hook_and_async_scope_disposal_order() => Context.SelfHooks.ShouldEqual(Context.ClassicHooks);
    [Fact] void should_not_claim_reducer_history_snapshots() => (Context.SelfHistoryEmpty && Context.ClassicHistoryEmpty).ShouldBeTrue();
    [Fact] void should_use_the_registered_container_in_mongodb()
    {
        // SQL lanes use sink reads above; direct MongoDB inspection belongs only to the MongoDB lane.
        Context.Stored?[nameof(SelfState.Total)].AsInt32.ShouldEqual(5);
    }

    [EventType]
    public record Added(int Value);
    [EventType]
    public record Deleted;
    public record SelfState(int Total, IReadOnlyList<int> Values)
    {
        public static Task<SelfState> Reduce(Added e, SelfState? current) => Task.FromResult(new SelfState((current?.Total ?? 0) + e.Value, [.. current?.Values ?? [], e.Value]));
        public static SelfState? Reduce(Deleted e, SelfState? current) => null;
        public static Task BeginReplay(Work<SelfState> work) => work.Record("begin");
        public static Task EndReplay(Work<SelfState> work) => work.Record("end");
        public static Task BeginReplayPartition(Partition partition, Work<SelfState> work) => work.Record("partition-begin");
        public static Task EndReplayPartition(Partition partition, Work<SelfState> work) => work.Record("partition-end");
    }
    public record ClassicState(int Total, IReadOnlyList<int> Values);
    public class Classic(Work<Classic> work) : IReducerFor<ClassicState>, ICanBeNotifiedWhenReplay, ICanBeNotifiedWhenPartitionReplayed
    {
        public Task<ClassicState> Apply(Added e, ClassicState? current) => Task.FromResult(new ClassicState((current?.Total ?? 0) + e.Value, [.. current?.Values ?? [], e.Value]));
        public ClassicState? Apply(Deleted e, ClassicState? current) => null;
        public Task BeginReplay() => work.Record("begin");
        public Task EndReplay() => work.Record("end");
        public Task BeginReplayPartition(Partition partition) => work.Record("partition-begin");
        public Task EndReplayPartition(Partition partition) => work.Record("partition-end");
    }
    public class Ledger
    {
        readonly ConcurrentDictionary<Type, ConcurrentQueue<string>> _traces = new();
        public ConcurrentQueue<string> For(Type type) => _traces.GetOrAdd(type, _ => new());
    }
    public class Work<T>(Ledger ledger) : IAsyncDisposable
    {
        bool _notified;
        public Task Record(string action)
        {
            _notified = true;
            ledger.For(typeof(T)).Enqueue(action);
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync()
        {
            if (_notified) ledger.For(typeof(T)).Enqueue("disposed");
            return ValueTask.CompletedTask;
        }
    }
}
