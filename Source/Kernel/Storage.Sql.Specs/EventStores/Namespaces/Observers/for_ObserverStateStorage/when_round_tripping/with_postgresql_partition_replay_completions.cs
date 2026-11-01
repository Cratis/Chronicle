// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

[Collection(PostgreSqlCollection.Name)]
public class with_postgresql_partition_replay_completions(PostgreSqlFixture fixture) : Specification
{
    KernelObserverState _state;
    KernelObserverState _saved;
    KernelObserverState _cleared;
    KernelObserverState _legacy;
    string _connectionString;

    void Establish() => _state = KernelObserverState.Empty with
    {
        Identifier = "observer",
        PartitionReplayCompletions =
        [
            new("partition", 42, 50, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch),
            new("other", 60, 65, null, DateTimeOffset.UnixEpoch.AddSeconds(1))
        ]
    };

    async Task Because()
    {
        // A database of its own: migrating the shared database would leave a later EnsureCreatedAsync there a no-op.
        _connectionString = await fixture.CreateMigratedDatabase<NamespaceDbContext>();
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>();
        builder.UseDatabaseFromConnectionString(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching();
        var options = builder.Options;
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new NamespaceDbContext(options), static () => { })));
        var storage = new ObserverStateStorage("event-store", "namespace", database);
        await storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await storage.Get("observer");
        await storage.Save(_state with { PartitionReplayCompletions = null }, Concepts.EventSequences.EventSequenceId.Log);
        _cleared = await storage.Get("observer");
        await storage.Save(KernelObserverState.Empty with { Identifier = "legacy" }, Concepts.EventSequences.EventSequenceId.Log);
        _legacy = await storage.Get("legacy");
    }

    [Fact] void should_keep_all_completion_fields() => _saved.PartitionReplayCompletions.ShouldEqual(_state.PartitionReplayCompletions);
    [Fact] void should_clear_the_completed_intents() => _cleared.PartitionReplayCompletions.ShouldBeNull();
    [Fact] void should_keep_legacy_intents_null() => _legacy.PartitionReplayCompletions.ShouldBeNull();

    async Task Destroy()
    {
        if (_connectionString is not null)
        {
            await fixture.DropDatabase(_connectionString);
        }
    }
}
