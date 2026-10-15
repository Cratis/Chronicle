// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public abstract class a_server_storage : for_UniqueConstraintMigrator.given.a_legacy_server_table
{
    protected UniqueConstraintsStorage _storage;
    protected UniqueConstraintDefinition _definition;

    protected async Task CreateStorage(string connectionString, bool sqlServer, Func<Task> dropDatabase)
    {
        await CreateLegacyTable(connectionString, sqlServer, dropDatabase);
        await _context.EnsureTableExists();
        var database = Substitute.For<IDatabase>();
        database.UniqueConstraintTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<UniqueConstraintDbContext>(new(_options, _tableName, _migrator), () => { })));
        _storage = new("store", "namespace", EventSequenceId.Log, database);
        _definition = new("unique", []);
    }
}
