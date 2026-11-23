// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage.given;

public abstract class a_provenance_database : Specification
{
    protected readonly List<string> _openedTables = [];
    protected bool _empty;
    protected bool _alias;
    protected bool _reloaded;
    protected bool _verify;
    protected bool _independent;
    protected Exception? _malformedError;
    protected Exception? _missingError;

    protected async Task Exercise(string connectionString, string independentConnection, string? aliasConnection = null)
    {
        var first = new SinkProvenanceStorage(DatabaseFor(connectionString), "store", "namespace");
        var alias = new SinkProvenanceStorage(DatabaseFor(aliasConnection ?? connectionString), "other-store", "other-namespace");
        _empty = await first.IsQuarantined();
        await Task.WhenAll(first.MarkQuarantined(), alias.MarkQuarantined());
        _alias = await alias.IsQuarantined();
        _reloaded = await new SinkProvenanceStorage(DatabaseFor(connectionString), "store", "namespace").IsQuarantined();
        _verify = await new SinkProvenanceStorage(DatabaseFor(connectionString, SchemaMode.Verify), "store", "namespace").IsQuarantined();
        var independent = new SinkProvenanceStorage(DatabaseFor(independentConnection), "store", "namespace");
        _independent = await independent.IsQuarantined();
        await new SinkProvenanceStorage(DatabaseFor(independentConnection, SchemaMode.Verify), "store", "namespace").MarkQuarantined();
        await Execute(independentConnection, "UPDATE \"__cratis_sink_provenance\" SET \"Version\" = 99");
        _malformedError = await Catch.Exception(independent.IsQuarantined);
        await Execute(independentConnection, "DROP TABLE \"__cratis_sink_provenance\"");
        _missingError = await Catch.Exception(() => new SinkProvenanceStorage(DatabaseFor(independentConnection, SchemaMode.Verify), "store", "namespace").IsQuarantined());
    }

    protected IDatabase DatabaseFor(string connectionString, SchemaMode mode = SchemaMode.Migrate)
    {
        var database = Substitute.For<IDatabase>();
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>(), Arg.Any<ConnectionPurpose>())
            .Returns(call => Open(connectionString, mode, (call.ArgAt<string>(2), call.ArgAt<IReadOnlyList<ProjectedColumn>>(3))));
        return database;
    }

    async Task<DbContextScope<ReadModelDbContext>> Open(string connectionString, SchemaMode mode, (string Table, IReadOnlyList<ProjectedColumn> Columns) shape)
    {
        _openedTables.Add(shape.Table);
        var context = Context(connectionString, mode, shape.Table, shape.Columns);
        try
        {
            await context.EnsureTableExists();
            return new(context, static () => { });
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    static ReadModelDbContext Context(string connectionString, SchemaMode mode, string table, IReadOnlyList<ProjectedColumn> columns)
    {
        var builder = new DbContextOptionsBuilder<ReadModelDbContext>(SpecDbContextOptions.Kernel<ReadModelDbContext>(connectionString));
        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new SchemaModeOptionsExtension(mode));
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(NullLogger<TableMigrator<ReadModelDbContext>>.Instance), NullLogger<ReadModelMigrator>.Instance);
        return new(builder.Options, table, columns, migrator);
    }

    protected static async Task Execute(string connectionString, string sql)
    {
        await using var context = Context(connectionString, SchemaMode.Migrate, "command", [new("Id", typeof(string), true, false, false, false)]);
        await context.Database.ExecuteSqlRawAsync(sql);
    }
}
