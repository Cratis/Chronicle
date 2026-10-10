// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesMigrations;

public class when_upgrading_an_event_store_database_from_before_visibility : Specification, IDisposable
{
    const string Added = $"ES-{WellKnownTableNames.EventTypes}-{nameof(v19_36_0)}";

    SqliteConnection _connection;
    EventType _stored;
    bool _newColumnsExistedBefore;

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        await using var context = CreateContext();
        var assembly = context.GetService<IMigrationsAssembly>();
        var history = context.GetService<IHistoryRepository>();
        var sqlGenerator = context.GetService<IMigrationsSqlGenerator>();
        await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());

        // Construct the database as it was before this migration existed, with every earlier migration applied.
        foreach (var (id, type) in assembly.Migrations.Where(entry => string.CompareOrdinal(entry.Key, Added) < 0))
        {
            var migration = assembly.CreateMigration(type, context.Database.ProviderName);
            foreach (var command in sqlGenerator.Generate(migration.UpOperations))
            {
                await context.Database.ExecuteSqlRawAsync(command.CommandText);
            }

            await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.0")));
        }

        // Braces are doubled because raw SQL is run through a format string.
        await context.Database.ExecuteSqlRawAsync(
            $"INSERT INTO \"{WellKnownTableNames.EventTypes}\" (Id, Owner, Tombstone, Schemas, MigrationsJson, Source) VALUES ('stored-before-visibility', 1, 0, '{{{{\"1\":\"{{{{}}}}\"}}}}', '[]', 1)");
        _newColumnsExistedBefore = await ColumnExists(context, "Visibility") || await ColumnExists(context, "Origin");
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        _stored = await context.EventTypes.SingleAsync();
    }

    [Fact] void should_not_have_the_columns_before_the_upgrade() => _newColumnsExistedBefore.ShouldBeFalse();
    [Fact] void should_keep_the_existing_row() => _stored.Id.Value.ShouldEqual("stored-before-visibility");
    [Fact] void should_read_the_existing_row_as_unspecified() => _stored.Visibility.ShouldEqual(EventTypeVisibility.Unspecified);
    [Fact] void should_read_the_existing_row_with_no_origin() => _stored.Origin.ShouldEqual(string.Empty);

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    static async Task<bool> ColumnExists(EventStoreDbContext context, string name)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{WellKnownTableNames.EventTypes}') WHERE name = $name";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = name;
        command.Parameters.Add(parameter);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    EventStoreDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options);
}
