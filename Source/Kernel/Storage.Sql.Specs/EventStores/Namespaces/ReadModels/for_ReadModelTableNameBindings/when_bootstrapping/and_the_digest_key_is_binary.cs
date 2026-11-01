// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_bootstrapping;

public class and_the_digest_key_is_binary : given.a_sqlite_ledger
{
    string _idCollation;
    string _physicalCollation;

    async Task Establish() => await Reserve(_owner, "Orders");

    async Task Because()
    {
        _idCollation = await _context.Database.SqlQueryRaw<string>("SELECT x.coll AS Value FROM pragma_index_list('chronicle_read_model_table_names') i JOIN pragma_index_xinfo(i.name) x WHERE i.origin = 'pk' AND x.key = 1").SingleAsync();
        _physicalCollation = await _context.Database.SqlQueryRaw<string>("SELECT coll AS Value FROM pragma_index_xinfo('IX_chronicle_read_model_table_names_PhysicalName') WHERE key = 1").SingleAsync();
    }

    [Fact] void should_keep_binary_digest_lookup() => _idCollation.ShouldEqual("BINARY");
    [Fact] void should_inherit_nocase_for_physical_uniqueness() => _physicalCollation.ShouldEqual("NOCASE");
}
