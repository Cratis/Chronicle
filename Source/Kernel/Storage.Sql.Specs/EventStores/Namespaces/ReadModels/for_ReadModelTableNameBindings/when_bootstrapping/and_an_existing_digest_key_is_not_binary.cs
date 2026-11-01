// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_bootstrapping;

public class and_an_existing_digest_key_is_not_binary : given.a_sqlite_ledger
{
    Exception _error;

    async Task Establish()
    {
        await _context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE chronicle_read_model_table_names (
                Id TEXT COLLATE NOCASE NOT NULL PRIMARY KEY,
                EventStoreId TEXT NOT NULL,
                Namespace TEXT NOT NULL,
                ReadModelIdentifier TEXT NOT NULL,
                ContainerName TEXT NOT NULL,
                Role INTEGER NOT NULL,
                LogicalName TEXT NOT NULL,
                PhysicalName TEXT COLLATE NOCASE NOT NULL,
                State INTEGER NOT NULL,
                ReplayAttempt TEXT NULL);
            CREATE UNIQUE INDEX IX_chronicle_read_model_table_names_PhysicalName ON chronicle_read_model_table_names (PhysicalName);
            """);
        _context.Bindings.Add(ReadModelTableNameBinding.For(_owner, "Orders", ReadModelTableBindingState.Reserved));
        await _context.SaveChangesAsync();
    }

    async Task Because() => _error = await Catch.Exception(() => CreateLedger(CreateContext()).UnderLock(async () => (await _ledger.Find(_owner.Id))!));

    [Fact] void should_reject_the_incompatible_digest_key() => _error.ShouldBeOfExactType<ReadModelTableBindingsSchemaMismatch>();
    [Fact] async Task should_preserve_the_existing_owner() => (await _ledger.Read()).Single().Owner.ShouldEqual(_owner);
    [Fact] async Task should_not_rewrite_the_digest_key() => (await _context.Database.SqlQueryRaw<string>("SELECT x.coll AS Value FROM pragma_index_list('chronicle_read_model_table_names') i JOIN pragma_index_xinfo(i.name) x WHERE i.origin = 'pk' AND x.key = 1").SingleAsync()).ShouldEqual("NOCASE");
}
