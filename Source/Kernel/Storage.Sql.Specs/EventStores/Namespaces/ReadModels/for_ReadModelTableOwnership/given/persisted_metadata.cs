// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableOwnership.given;

public class persisted_metadata : for_ReadModelTableNameBindings.given.a_sqlite_ledger
{
    private protected EventStoreDbContext _definitions;
    private protected NamespaceDbContext _history;
    private protected readonly List<ReadModelTableMetadataScope> _scopes = [];
    private protected ReadModelTableOwnership _ownership;
    private protected IReadOnlyList<ReadModelTableClaim> _claims;
    private protected IReadOnlyList<ReadModelTableMetadataScope> _unknown;

    async Task Establish()
    {
        _definitions = new(new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(_connectionString).AddConceptAsSupport().Options);
        _history = new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connectionString).AddConceptAsSupport().Options);
        await _context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE ReadModelDefinitions (Id TEXT NOT NULL PRIMARY KEY, Name TEXT NOT NULL, SinkType TEXT NOT NULL);
            INSERT INTO ReadModelDefinitions VALUES ('model', 'orders', 'SQL');
            CREATE TABLE ReplayContexts (ReadModelIdentifier TEXT NOT NULL PRIMARY KEY, Generation INTEGER NOT NULL, ReadModel TEXT NOT NULL, RevertModel TEXT NOT NULL, Started TEXT NOT NULL);
            CREATE TABLE ReplayedReadModels (ObserverId TEXT NOT NULL, ReadModelIdentifier TEXT NOT NULL, Generation INTEGER NOT NULL, ReadModelName TEXT NOT NULL, RevertModelName TEXT NOT NULL, Started TEXT NOT NULL);
            CREATE TABLE chronicle_replay_promotions (Id TEXT NOT NULL PRIMARY KEY, ContainerName TEXT NOT NULL, RevertContainerName TEXT NOT NULL, BackupTableName TEXT NULL);
            """);
        _scopes.Add(new("store", "default", _definitions, _history));
        _ownership = new(
            () => Task.FromResult<IReadOnlyList<ReadModelTableMetadataScope>>(_scopes),
            _context,
            new ReadModelTableCatalog(_context, "main"),
            _ => Task.FromResult(true));
    }

    void Destroy()
    {
        _definitions.Dispose();
        _history.Dispose();
    }

    protected Task AddOccurrence() => _context.Database.ExecuteSqlRawAsync("""
        INSERT INTO ReplayedReadModels VALUES ('observer', 'model', 1, 'original-orders', 'original-orders-old', '2026-06-07 12:00:00+00:00')
        """);
}
