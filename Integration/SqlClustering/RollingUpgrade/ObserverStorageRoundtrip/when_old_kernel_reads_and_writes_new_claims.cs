// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using KernelConceptTypes = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;
using SqlObserverState = Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.ObserverState;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.ObserverStorageRoundtrip;

/// <summary>
/// Proves the pinned released writer can load and save a new nullable-claims schema without erasing shadow data.
/// </summary>
public class when_old_kernel_reads_and_writes_new_claims : Specification
{
    ReleasedSqlKernelFixture _fixture;
    DbContextOptions<NamespaceDbContext> _options;
    ObserverStateStorage _storage;
    KernelObserverState _before;
    KernelObserverState _after;
    SqlObserverState _rowBefore;
    SqlObserverState _rowAfter;
    string _connectionString;
    string _identifier;
    bool _oldWriterUpdated;

    async Task Establish()
    {
        _fixture = new();
        await _fixture.Start();
        var selected = _fixture.Observer;
        _connectionString = selected.ConnectionString;
        _identifier = selected.Identifier;
        await _fixture.StopKernel();
        _options = new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseNpgsql(_connectionString)
            .AddConceptAsSupport()
            .EnableServiceProviderCaching(false)
            .Options;
        await using (var context = new NamespaceDbContext(_options))
        {
            await context.Database.MigrateAsync();
        }
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<KernelConceptTypes.EventStoreName>(), Arg.Any<KernelConceptTypes.EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new NamespaceDbContext(_options), static () => { })));
        _storage = new("old-kernel-store", "old-kernel-namespace", database);
        _before = await _storage.Get(_identifier);
        await _storage.Save(_before with
        {
            PartitionClaims = [new("shadow-partition", ClaimHolderKind.PartitionCatchUp, new(Guid.NewGuid()), new(3), 42, [])],
            ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
            OwnershipRevision = new(7)
        }, new(selected.Owner));
        _rowBefore = await Row();
        await ReleasedObserverDatabase.WitnessUpdates(_connectionString);
    }

    async Task Because()
    {
        await _fixture.RestartKernel();
        _oldWriterUpdated = await ReleasedObserverDatabase.WasUpdated(_connectionString, _identifier);
        await _fixture.StopKernel();
        _after = await _storage.Get(_identifier);
        _rowAfter = await Row();
    }

    [Fact] void should_require_an_actual_old_writer_update() => _oldWriterUpdated.ShouldBeTrue();
    [Fact] void should_preserve_the_legacy_identity() => _after.Identifier.ShouldEqual(_before.Identifier);
    [Fact] void should_preserve_legacy_subscription_mode() => _after.SubscribesToAllEvents.ShouldEqual(_before.SubscribesToAllEvents);
    [Fact] void should_preserve_the_last_handled_event() => _after.LastHandledEventSequenceNumber.ShouldEqual(_before.LastHandledEventSequenceNumber);
    [Fact] void should_preserve_the_next_event() => _after.NextEventSequenceNumber.ShouldEqual(_before.NextEventSequenceNumber);
    [Fact] void should_preserve_handled_count() => _after.HandledEventCount.ShouldEqual(_before.HandledEventCount);
    [Fact] void should_preserve_legacy_replay_sets() => _after.ReplayingPartitions.ShouldContainOnly(_before.ReplayingPartitions);
    [Fact] void should_preserve_legacy_partition_sets() => _after.CatchingUpPartitions.ShouldContainOnly(_before.CatchingUpPartitions);
    [Fact] void should_preserve_partition_claim_json_byte_for_byte() => _rowAfter.PartitionClaims.ShouldEqual(_rowBefore.PartitionClaims);
    [Fact] void should_preserve_wide_claim_json_byte_for_byte() => _rowAfter.ObserverWideClaim.ShouldEqual(_rowBefore.ObserverWideClaim);
    [Fact] void should_preserve_the_revision() => _after.OwnershipRevision.ShouldEqual(new OwnershipRevision(7));
    [Fact] void should_remain_readable_by_the_new_provider() => _after.PartitionClaims.Single().Epoch.ShouldEqual(new ClaimEpoch(3));

    async Task Cleanup()
    {
        if (_fixture is not null) await _fixture.DisposeAsync();
    }

    async Task<SqlObserverState> Row()
    {
        await using var context = new NamespaceDbContext(_options);
        return await context.Observers.AsNoTracking().SingleAsync(row => row.Id == _identifier);
    }
}
