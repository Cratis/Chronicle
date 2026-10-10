// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public class a_per_value_constraint : Specification
{
    protected UniqueConstraintsStorage _storage;
    protected UniqueConstraintDefinition _definition;
    protected EventSourceId _owner;
    protected EventSourceId _otherOwner;
    SqliteConnection _connection;
    IUniqueConstraintMigrator _migrator;

    void Establish()
    {
        _connection = new($"DataSource=per-value-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();
        _migrator = new UniqueConstraintMigrator(
            Substitute.For<ITableMigrator<UniqueConstraintDbContext>>(),
            new TableMigrator<UniqueConstraintValuesDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintValuesDbContext>>>()),
            Substitute.For<ILogger<UniqueConstraintMigrator>>());

        var database = Substitute.For<IDatabase>();
        database.UniqueConstraintValuesTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(async call =>
            {
                var context = CreateContext(call.ArgAt<string>(2));
                await context.EnsureTableExists();
                return new DbContextScope<UniqueConstraintValuesDbContext>(context, () => { });
            });
        _storage = new("store", "namespace", EventSequenceId.Log, database);
        _definition = new("versions", []) { Mode = UniqueConstraintMode.PerValue };
        _owner = EventSourceId.New();
        _otherOwner = EventSourceId.New();
    }

    void Destroy() => _connection.Dispose();

    UniqueConstraintValuesDbContext CreateContext(string tableName) => new(
        new DbContextOptionsBuilder<UniqueConstraintValuesDbContext>().UseSqlite(_connection.ConnectionString).AddConceptAsSupport().Options,
        tableName,
        _migrator);
}
