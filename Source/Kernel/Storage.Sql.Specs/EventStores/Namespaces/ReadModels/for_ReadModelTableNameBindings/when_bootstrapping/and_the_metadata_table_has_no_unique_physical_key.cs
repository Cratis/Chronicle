// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_bootstrapping;

public class and_the_metadata_table_has_no_unique_physical_key : given.a_sqlite_ledger
{
    Exception _error;

    async Task Establish()
    {
        await Reserve(_owner, "orders");
        await _context.Database.ExecuteSqlRawAsync("DROP INDEX IX_chronicle_read_model_table_names_PhysicalName");
    }

    async Task Because() => _error = await Catch.Exception(() => CreateLedger(CreateContext()).UnderLock(async () => (await _ledger.Find(_owner.Id))!));

    [Fact] void should_not_trust_the_unprotected_ledger() => _error.ShouldBeOfExactType<ReadModelTableBindingsSchemaMismatch>();
    [Fact] async Task should_preserve_the_existing_binding() => (await _ledger.Read()).Single().Owner.ShouldEqual(_owner);
}
