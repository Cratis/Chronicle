// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTables.when_opening;

public class and_a_backup_is_only_reserved : given.a_sqlite_placement
{
    readonly ReadModelTableHandle _backup = ReadModelTableHandle.For("store", "default", "model", "orders", ReadModelTableRole.Backup, "orders-20261101");
    Exception? _error;
    ReadModelTableNameBinding? _binding;

    async Task Establish() => await _tables.Reserve(_backup);

    async Task Because()
    {
        _error = await Catch.Exception(async () =>
        {
            await using var scope = await _tables.Open(_backup, _columns);
        });
        _binding = await _tables.Find(_backup);
    }

    [Fact] void should_refuse_to_create_a_backup_on_demand() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_leave_it_reserved_until_promotion() => _binding!.State.ShouldEqual(ReadModelTableBindingState.Reserved);
}
