// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_ensuring_a_sqlite_read_model_table;

public class and_replay_dropped_the_empty_table : given.a_sqlite_read_model_table
{
    int _rows;

    async Task Because()
    {
        await using (var original = await ReadModelTable())
        {
            await original.DbContext.Database.ExecuteSqlRawAsync("DROP TABLE \"replay-model\"");
        }

        await using var recreated = await ReadModelTable();
        _rows = await recreated.DbContext.Entries.CountAsync();
    }

    [Fact] void should_recreate_the_table_on_the_next_acquisition() => _rows.ShouldEqual(0);
}
