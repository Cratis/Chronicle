// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.given;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.for_SpecDbContextOptions;

public class when_building_kernel_options_for_many_databases : Specification
{
    string[] _databases;
    IModelSource[] _namespaceSources;
    IModelSource[] _eventStoreSources;
    IModelSource[] _clusterSources;
    IModelSource[] _eventSequenceSources;
    IModelSource[] _readModelSources;
    IModelSource[] _interceptedSources;
    IModelSource[] _uncachedSources;
    IModelSource[] _inspectionSources;
    IModelSource[] _schemaLockSources;
    IModelSource[] _uniqueConstraintSources;

    void Establish() => _databases = Enumerable.Range(0, 25)
        .Select(index => $"Host=localhost;Database=cache_guard_{index};Username=postgres;Password=postgres")
        .ToArray();

    void Because()
    {
        ProjectedColumn[] readModelColumns = [new("Id", typeof(string), IsKey: true, IsJson: false, IsArray: false, IsNullable: false)];
        _namespaceSources = ModelSources(connection => new NamespaceDbContext(SpecDbContextOptions.Kernel<NamespaceDbContext>(connection)));
        _eventStoreSources = ModelSources(connection => new EventStoreDbContext(SpecDbContextOptions.Kernel<EventStoreDbContext>(connection)));
        _clusterSources = ModelSources(connection => new ClusterDbContext(new DbContextOptionsBuilder<ClusterDbContext>(SpecDbContextOptions.Kernel<ClusterDbContext>(connection))
            .UseNpgsql(connection, options => options.EnableRetryOnFailure(2, TimeSpan.Zero, null)).Options));
        _eventSequenceSources = ModelSources(connection => new EventSequenceDbContext(SpecDbContextOptions.Kernel<EventSequenceDbContext>(connection), "events", Substitute.For<IEventSequenceMigrator>()));
        _readModelSources = ModelSources(connection => new ReadModelDbContext(SpecDbContextOptions.ForConnection<ReadModelDbContext>(connection), "read_model", readModelColumns, Substitute.For<IReadModelMigrator>()));
        _inspectionSources = ModelSources(connection => new DbContext(SpecDbContextOptions.ForConnection<DbContext>(connection)));
        _schemaLockSources = ModelSources(connection => new SchemaLockDbContext(SpecDbContextOptions.Kernel<SchemaLockDbContext>(connection)));
        _uniqueConstraintSources = ModelSources(connection => new UniqueConstraintDbContext(SpecDbContextOptions.Kernel<UniqueConstraintDbContext>(connection), "claims", Substitute.For<IUniqueConstraintMigrator>()));
        _interceptedSources = ModelSources(connection => new NamespaceDbContext(new DbContextOptionsBuilder<NamespaceDbContext>(SpecDbContextOptions.Kernel<NamespaceDbContext>(connection))
            .AddInterceptors(new ControlWriteBarrier()).Options));
        _uncachedSources = ModelSources(
            connection => new NamespaceDbContext(new DbContextOptionsBuilder<NamespaceDbContext>(SpecDbContextOptions.Kernel<NamespaceDbContext>(connection))
                .WithoutServiceProviderCaching().Options),
            _databases.Take(2));
    }

    [Fact] void should_probe_twenty_five_distinct_databases() => _databases.Distinct().Count().ShouldEqual(25);
    [Fact] void should_share_namespace_services() => _namespaceSources.Distinct(ReferenceEqualityComparer.Instance).Count().ShouldEqual(1);
    [Fact] void should_share_event_store_services() => _eventStoreSources.Distinct(ReferenceEqualityComparer.Instance).Count().ShouldEqual(1);
    [Fact] void should_share_membership_services_with_retries() => _clusterSources.Distinct(ReferenceEqualityComparer.Instance).Count().ShouldEqual(1);
    [Fact] void should_share_event_sequence_services() => _eventSequenceSources.Distinct(ReferenceEqualityComparer.Instance).Count().ShouldEqual(1);
    [Fact] void should_share_read_model_services() => _readModelSources.Distinct(ReferenceEqualityComparer.Instance).Count().ShouldEqual(1);
    [Fact] void should_share_inspection_services_with_the_kernel() => _inspectionSources.All(source => ReferenceEquals(source, _namespaceSources[0])).ShouldBeTrue();
    [Fact] void should_share_schema_lock_services_with_the_kernel() => _schemaLockSources.All(source => ReferenceEquals(source, _namespaceSources[0])).ShouldBeTrue();
    [Fact] void should_share_unique_constraint_services() => _uniqueConstraintSources.Distinct(ReferenceEqualityComparer.Instance).Count().ShouldEqual(1);
    [Fact] void should_share_services_with_per_context_interceptors() => _interceptedSources.Distinct(ReferenceEqualityComparer.Instance).Count().ShouldEqual(1);
    [Fact] void should_detect_distinct_providers_when_caching_is_disabled() => _uncachedSources[0].ShouldNotBeSame(_uncachedSources[1]);

    IModelSource[] ModelSources(Func<string, DbContext> create, IEnumerable<string>? connections = null) => (connections ?? _databases)
        .Select(connection =>
        {
            using var context = create(connection);
            return context.GetService<IModelSource>();
        }).ToArray();
}
