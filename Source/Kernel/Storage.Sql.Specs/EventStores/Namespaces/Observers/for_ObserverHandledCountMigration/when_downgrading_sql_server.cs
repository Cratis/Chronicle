// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverHandledCountMigration;

[Collection(SqlServerCollection.Name)]
public class when_downgrading_sql_server(SqlServerFixture fixture) : Specification
{
    NamespaceDbContext _context;
    string _connectionString;
    Migrations.v19_110_0 _migration;
    int _upgradedJsonConstraints;
    object _countsPerEventType;
    ulong _count;
    int _remainingColumns;
    int _remainingJsonConstraints;
    string _identifier;
    long _lastHandledEventSequenceNumber;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>();

        // Match Database.BuildOptions so Arc's SQL generator creates the JSON CHECK constraints.
        builder.UseDatabaseFromConnectionString(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching();
        _context = new(builder.Options);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE [Observers] ([Id] nvarchar(255) NOT NULL PRIMARY KEY, [LastHandledEventSequenceNumber] bigint NOT NULL)");
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO [Observers] ([Id], [LastHandledEventSequenceNumber]) VALUES ('legacy-observer', 42)");
        _migration = new() { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(_migration.UpOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }

        await _context.Database.OpenConnectionAsync();
        await using var read = _context.Database.GetDbConnection().CreateCommand();
        read.CommandText = "SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'Observers') AND name = 'CK_Observers_HandledEventCountPerEventType_IsJson'";
        _upgradedJsonConstraints = (int)(await read.ExecuteScalarAsync())!;
        read.CommandText = "SELECT [HandledEventCountPerEventType] FROM [Observers] WHERE [Id] = 'legacy-observer'";
        _countsPerEventType = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT [HandledEventCount] FROM [Observers] WHERE [Id] = 'legacy-observer'";
        _count = Convert.ToUInt64(await read.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    async Task Because()
    {
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(_migration.DownOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }

        await using var read = _context.Database.GetDbConnection().CreateCommand();
        read.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'Observers') AND name IN ('HandledEventCount', 'HandledEventCountPerEventType')";
        _remainingColumns = (int)(await read.ExecuteScalarAsync())!;
        read.CommandText = "SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'Observers') AND name = 'CK_Observers_HandledEventCountPerEventType_IsJson'";
        _remainingJsonConstraints = (int)(await read.ExecuteScalarAsync())!;
        read.CommandText = "SELECT [Id] FROM [Observers]";
        _identifier = (string)await read.ExecuteScalarAsync();
        read.CommandText = "SELECT [LastHandledEventSequenceNumber] FROM [Observers] WHERE [Id] = 'legacy-observer'";
        _lastHandledEventSequenceNumber = (long)(await read.ExecuteScalarAsync())!;
    }

    [Fact] void should_upgrade_with_the_production_json_constraint() => _upgradedJsonConstraints.ShouldEqual(1);
    [Fact] void should_leave_legacy_counts_per_event_type_absent_after_upgrade() => _countsPerEventType.ShouldEqual(DBNull.Value);
    [Fact] void should_default_the_legacy_handled_event_count_to_zero() => _count.ShouldEqual(0UL);
    [Fact] void should_remove_both_added_columns() => _remainingColumns.ShouldEqual(0);
    [Fact] void should_remove_the_json_constraint() => _remainingJsonConstraints.ShouldEqual(0);
    [Fact] void should_preserve_the_legacy_observer() => _identifier.ShouldEqual("legacy-observer");
    [Fact] void should_preserve_the_legacy_position() => _lastHandledEventSequenceNumber.ShouldEqual(42L);

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await fixture.DropDatabase(_connectionString);
    }
}
