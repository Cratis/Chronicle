// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_bootstrapping;

public class and_an_old_physical_key_uses_binary_equality : given.a_sqlite_ledger
{
    Exception _error;

    async Task Establish()
    {
        await Reserve(_owner, "Orders");
        await _context.Database.ExecuteSqlRawAsync("DROP INDEX IX_chronicle_read_model_table_names_PhysicalName");
        await _context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_chronicle_read_model_table_names_PhysicalName ON chronicle_read_model_table_names (PhysicalName COLLATE BINARY)");
    }

    async Task Because() => _error = await Catch.Exception(() => CreateLedger(CreateContext()).UnderLock(async () => (await _ledger.Find(_owner.Id))!));

    [Fact] void should_fail_loudly_instead_of_upgrading_the_ledger() => _error.ShouldBeOfExactType<ReadModelTableBindingsSchemaMismatch>();
    [Fact] async Task should_preserve_the_original_owner() => (await _ledger.Read()).Single().Owner.ShouldEqual(_owner);
    [Fact] async Task should_preserve_the_original_physical_name() => (await _ledger.Read()).Single().PhysicalName.ShouldEqual("Orders");
    [Fact] async Task should_not_rebuild_the_old_index() => (await _context.Database.SqlQueryRaw<string>("SELECT coll AS Value FROM pragma_index_xinfo('IX_chronicle_read_model_table_names_PhysicalName') WHERE key = 1").SingleAsync()).ShouldEqual("BINARY");
}
