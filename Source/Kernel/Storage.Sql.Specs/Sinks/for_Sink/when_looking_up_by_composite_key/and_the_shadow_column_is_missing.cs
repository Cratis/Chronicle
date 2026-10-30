// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_the_shadow_column_is_missing : given.a_composite_key_lookup_table
{
    int _columns;
    async Task Establish()
    {
        await Write("a", "b", "initial");
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("DROP TABLE composite_shadows; CREATE TABLE composite_shadows(id TEXT NOT NULL PRIMARY KEY, name TEXT NULL, __lastHandledEventSequenceNumber INTEGER NULL, __initialized INTEGER NOT NULL DEFAULT 1); INSERT INTO composite_shadows(id,name) VALUES ('a_b','old kernel')");
    }
    async Task Because()
    {
        _result = await Lookup("a", "b");
        await using var context = CreateContext();
        _columns = await context.Database.SqlQuery<int>($"SELECT count(*) AS Value FROM pragma_table_xinfo('composite_shadows') WHERE name='__compositeKey'").SingleAsync();
    }
    [Fact] void should_find_the_legacy_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_repair_the_shadow() => _columns.ShouldEqual(1);
}
