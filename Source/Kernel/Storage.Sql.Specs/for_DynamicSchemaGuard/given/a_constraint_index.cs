// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard.given;

public class a_constraint_index : for_Database.given.a_sqlite_database
{
    protected const string Table = "guarded_claims";
    protected const string Index = "IX_guarded_claims_Value";
    protected UniqueConstraintDbContext _context;
    private protected readonly SchemaLogger<DynamicSchemaGuard> _guardLogger = new();
    protected readonly ILogger<UniqueConstraintMigrator> _migrationLogger = Substitute.For<ILogger<UniqueConstraintMigrator>>();

    async Task Establish()
    {
        var options = new DbContextOptionsBuilder<UniqueConstraintDbContext>().UseSqlite(_connectionString)
            .AddConceptAsSupport().WithoutServiceProviderCaching();
        ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(new SchemaModeOptionsExtension(SchemaMode.Verify, new DynamicSchemaGuard(_guardLogger)));
        _context = new(options.Options, Table, Substitute.For<IUniqueConstraintMigrator>());
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE guarded_claims (EventSourceId TEXT NOT NULL PRIMARY KEY, Value TEXT NOT NULL)");
    }

    protected Task EnsureIndex() => UniqueConstraintValueIndexMigration.EnsureValueIndexIsUnique(Table, _context, _migrationLogger);

    protected async Task<Exception?> Inspect()
    {
        using var inspection = DynamicSchemaVerification.Begin();
        return await Catch.Exception(async () =>
        {
            await EnsureIndex();
            DynamicSchemaVerification.Complete("test");
        });
    }

    async Task Destroy() => await _context.DisposeAsync();
}
