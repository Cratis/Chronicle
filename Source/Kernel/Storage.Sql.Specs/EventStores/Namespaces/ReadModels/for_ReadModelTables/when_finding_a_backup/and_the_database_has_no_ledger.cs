// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTables.when_finding_a_backup;

public class and_the_database_has_no_ledger : given.a_sqlite_placement
{
    ReadModelTableNameBinding? _binding;
    Exception? _error;

    async Task Because()
    {
        var backup = ReadModelTableHandle.For("store", "default", "model", "orders", ReadModelTableRole.Backup, "orders_revert");
        _error = await Catch.Exception(async () => _binding = await _tables.FindBackup(backup));
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_find_no_backup() => _binding.ShouldBeNull();
}
