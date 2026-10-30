// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverClaimsMigration;

public class when_upgrading_sqlite : Specification, IDisposable
{
    SqliteConnection _connection;
    NamespaceDbContext _context;
    object _claims;
    object _wide;
    object _revision;
    string _identifier;

    async Task Establish()
    {
        _connection = new("DataSource=:memory:");
        await _connection.OpenAsync();
        _context = new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE Observers (Id TEXT NOT NULL PRIMARY KEY)");
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO Observers (Id) VALUES ('legacy-observer')");
    }

    async Task Because()
    {
        var migration = new Migrations.v19_112_0_1 { ActiveProvider = _context.Database.ProviderName };
        foreach (var command in _context.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations))
        {
            await _context.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await using var read = _connection.CreateCommand();
        read.CommandText = "SELECT PartitionClaims FROM Observers WHERE Id = 'legacy-observer'";
        _claims = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT ObserverWideClaim FROM Observers WHERE Id = 'legacy-observer'";
        _wide = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT OwnershipRevision FROM Observers WHERE Id = 'legacy-observer'";
        _revision = await read.ExecuteScalarAsync();
        read.CommandText = "SELECT Id FROM Observers";
        _identifier = (string)await read.ExecuteScalarAsync();
    }

    [Fact] void should_leave_legacy_partition_claims_null() => _claims.ShouldEqual(DBNull.Value);
    [Fact] void should_leave_legacy_observer_wide_claim_null() => _wide.ShouldEqual(DBNull.Value);
    [Fact] void should_leave_legacy_revision_null() => _revision.ShouldEqual(DBNull.Value);
    [Fact] void should_preserve_existing_rows() => _identifier.ShouldEqual("legacy-observer");

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
