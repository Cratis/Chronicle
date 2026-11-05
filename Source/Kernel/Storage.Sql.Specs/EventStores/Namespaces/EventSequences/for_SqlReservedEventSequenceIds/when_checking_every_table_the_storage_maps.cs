// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Choreography.Storage.Sql.Jobs;
using Cratis.Choreography.Storage.Sql.Patching;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_SqlReservedEventSequenceIds;

public class when_checking_every_table_the_storage_maps : Specification
{
    const string SequenceTable = "probe_sequence";
    const string ReadModelTable = "probe_read_model";
    readonly SqlReservedEventSequenceIds _reserved = new();
    HashSet<string> _namespaceTables;
    HashSet<string> _eventStoreTables;
    HashSet<string> _clusterTables;
    HashSet<string> _sequenceTables;
    HashSet<string> _readModelTables;
    HashSet<string> _jobTables;
    HashSet<string> _patchTables;

    void Because()
    {
        _namespaceTables = Tables(new NamespaceDbContext(Options<NamespaceDbContext>()));
        _eventStoreTables = Tables(new EventStoreDbContext(Options<EventStoreDbContext>()));
        _clusterTables = Tables(new ClusterDbContext(Options<ClusterDbContext>()));
        _sequenceTables = Tables(new EventSequenceDbContext(Options<EventSequenceDbContext>(), SequenceTable, Substitute.For<IEventSequenceMigrator>()));
        ProjectedColumn[] columns = [new("Id", typeof(string), IsKey: true, IsJson: false, IsArray: false, IsNullable: false)];
        _readModelTables = Tables(new ReadModelDbContext(Options<ReadModelDbContext>(), ReadModelTable, columns, Substitute.For<IReadModelMigrator>()));
        _jobTables = Tables(new JobsDbContext(Options<JobsDbContext>()));
        _patchTables = Tables(new PatchesDbContext(Options<PatchesDbContext>()));
    }

    [Fact] void should_find_tables_to_check() => _namespaceTables.ShouldContain("EventSequences");

    [Fact] void should_reserve_every_namespace_table() => Unreserved(_namespaceTables).ShouldBeEmpty();

    [Fact] void should_reserve_every_event_store_table() => Unreserved(_eventStoreTables).ShouldBeEmpty();

    [Fact] void should_reserve_every_cluster_table() => Unreserved(_clusterTables).ShouldBeEmpty();

    [Fact] void should_reserve_every_event_sequence_companion_table() => Unreserved(_sequenceTables).ShouldBeEmpty();

    [Fact] void should_reserve_every_read_model_companion_table() => Unreserved(_readModelTables).ShouldBeEmpty();

    [Fact] void should_reserve_every_job_table() => Unreserved(_jobTables).ShouldBeEmpty();

    [Fact] void should_reserve_every_patch_table() => Unreserved(_patchTables).ShouldBeEmpty();

    static DbContextOptions<TContext> Options<TContext>()
        where TContext : DbContext => new DbContextOptionsBuilder<TContext>()
            .UseSqlite("DataSource=:memory:")
            .AddConceptAsSupport().WithoutServiceProviderCaching().Options;

    static HashSet<string> Tables(DbContext context)
    {
        using (context)
        {
            return context.Model.GetEntityTypes().Select(entity => entity.GetTableName())
                .OfType<string>().Where(name => name != SequenceTable && name != ReadModelTable)
                .ToHashSet(StringComparer.Ordinal);
        }
    }

    IEnumerable<string> Unreserved(IEnumerable<string> tables) => tables.Where(name => !_reserved.IsReserved(new Concepts.EventSequences.EventSequenceId(name)));
}
