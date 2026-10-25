// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

[Collection(PostgreSqlCollection.Name)]
public class with_postgresql_partition_claims(PostgreSqlFixture fixture) : Specification
{
    KernelObserverState _state;
    KernelObserverState _saved;
    KernelObserverState _legacy;

    void Establish() => _state = KernelObserverState.Empty with
    {
        Identifier = "observer",
        PartitionClaims =
        [
            new("partition", ClaimHolderKind.PartitionCatchUp, null, new(3), 42, []),
            new("other", ClaimHolderKind.PartitionReplay, new(Guid.NewGuid()), new(6), 47, [])
        ],
        ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
        OwnershipRevision = new(7)
    };

    async Task Because()
    {
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>();
        builder.UseDatabaseFromConnectionString(fixture.ConnectionString).AddConceptAsSupport().WithoutServiceProviderCaching();
        var options = builder.Options;
        await using var context = new NamespaceDbContext(options);
        await context.Database.MigrateAsync();
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new NamespaceDbContext(options), static () => { })));
        var storage = new ObserverStateStorage("event-store", "namespace", database);
        await storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);
        _saved = await storage.Get("observer");
        await storage.Save(KernelObserverState.Empty with { Identifier = "legacy" }, Concepts.EventSequences.EventSequenceId.Log);
        _legacy = await storage.Get("legacy");
    }

    [Fact] void should_keep_partitions() => _saved.PartitionClaims.Select(claim => claim.Partition).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Partition));
    [Fact] void should_keep_jobs() => _saved.PartitionClaims.Select(claim => claim.Job).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Job));
    [Fact] void should_keep_epochs() => _saved.PartitionClaims.Select(claim => claim.Epoch).ShouldEqual(_state.PartitionClaims.Select(claim => claim.Epoch));
    [Fact] void should_keep_the_wide_job() => _saved.ObserverWideClaim.Job.ShouldEqual(_state.ObserverWideClaim.Job);
    [Fact] void should_keep_the_revision() => _saved.OwnershipRevision.ShouldEqual(new OwnershipRevision(7));
    [Fact] void should_keep_legacy_claims_null() => _legacy.PartitionClaims.ShouldBeNull();
    [Fact] void should_keep_legacy_wide_claim_null() => _legacy.ObserverWideClaim.ShouldBeNull();
    [Fact] void should_keep_legacy_revision_null() => _legacy.OwnershipRevision.ShouldBeNull();
}
