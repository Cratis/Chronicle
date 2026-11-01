// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTables.when_opening;

public class and_a_ready_live_table_was_dropped : given.a_sqlite_placement
{
    Exception? _error;
    ReadModelTableNameBinding? _binding;

    async Task Establish()
    {
        await using var scope = await _tables.Open(_live, _columns);
        await scope.DbContext.Database.ExecuteSqlRawAsync("DROP TABLE orders");
    }

    async Task Because()
    {
        _error = await Catch.Exception(async () =>
        {
            await using var scope = await _tables.Open(_live, _columns);
        });
        _binding = await _tables.Find(_live);
    }

    [Fact] void should_refuse_to_create_a_replacement() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_not_rewrite_the_ready_binding() => _binding!.State.ShouldEqual(ReadModelTableBindingState.Ready);
}
