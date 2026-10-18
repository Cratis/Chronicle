// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

using KernelFailedPartition = Cratis.Chronicle.Concepts.Observation.FailedPartition;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

[Collection(PostgreSqlCollection.Name)]
public class with_postgresql_failed_partition_recovery(PostgreSqlFixture fixture) : Specification
{
    string _connectionString;
    ObserverStateStorage _storage;
    KernelFailedPartition _partition;
    KernelFailedPartition _restored;
    ObserverInitialization _initialization;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), () => { })));
        _storage = new(EventStoreName.System, EventStoreNamespaceName.Default, database);
        _partition = new()
        {
            ObserverId = "observer",
            Partition = "partition",
            IsQuarantined = true,
            AttemptsBeforeBudgetReset = 1,
            RecoveryCursor = new(new(51), FailedPartitionRecoveryMode.Replay, new(80)),
            RecoveryOwner = new(Guid.NewGuid(), Guid.NewGuid(), 3, FailedPartitionRecoveryState.Resuming, 7),
            Hold = new(FailedPartitionHoldReason.WaitingForHandler, new("event-a"), new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero)),
            Attempts =
            [
                new() { SequenceNumber = new(49), Kind = FailureKind.Handling },
                new() { SequenceNumber = new(50), Kind = FailureKind.Handling }
            ]
        };
    }

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", FailedPartitions = [_partition], Initialization = new(ObserverInitializationState.NotInitialized, ["unfinished job", "unsettled handover"]) });
        var saved = await _storage.Get("observer");
        _restored = saved.FailedPartitions.Single();
        _initialization = saved.Initialization;
    }

    [Fact] void should_keep_initialization() => _initialization.State.ShouldEqual(ObserverInitializationState.NotInitialized);
    [Fact] void should_keep_every_refusal_reason() => _initialization.RefusalReasons.ShouldEqual("unfinished job", "unsettled handover");
    [Fact] void should_keep_the_exact_cursor_and_replay_boundary() => _restored.RecoveryCursor.ShouldEqual(_partition.RecoveryCursor);
    [Fact] void should_keep_the_whole_recovery_owner() => _restored.RecoveryOwner.ShouldEqual(_partition.RecoveryOwner);
    [Fact] void should_keep_the_hold_reason_type_and_start() => _restored.Hold.ShouldEqual(_partition.Hold);
    [Fact] void should_keep_attempts() => _restored.Attempts.Count().ShouldEqual(2);
    [Fact] void should_keep_the_last_attempt() => _restored.LastAttempt.SequenceNumber.Value.ShouldEqual(50UL);
    [Fact] void should_keep_the_attempts_before_the_budget_reset() => _restored.AttemptsBeforeBudgetReset.ShouldEqual(1);
    [Fact] void should_keep_the_current_retry_budget() => _restored.AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_keep_quarantine() => _restored.IsQuarantined.ShouldBeTrue();

    async Task Destroy() => await fixture.DropDatabase(_connectionString);

    NamespaceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>();
        builder.UseDatabaseFromConnectionString(_connectionString).AddConceptAsSupport();
        return new(builder.Options);
    }
}
