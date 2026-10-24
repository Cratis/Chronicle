// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

public class and_the_row_was_written_by_an_older_kernel : given.a_composite_key_sink
{
    string _stamp;
    string _id;

    protected override ISqlSinkHarness CreateHarness() => new SqlSinkHarness();

    async Task Because()
    {
        await Write("other", "row", "initial");
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("INSERT INTO composite_shadows(id,name) VALUES ('a_b','legacy')");
        await Write("a", "b", "current");
        _stamp = await context.Database.SqlQuery<string>($"SELECT __compositeKey AS Value FROM composite_shadows WHERE id='a_b'").SingleAsync();
        _id = await context.Database.SqlQuery<string>($"SELECT id AS Value FROM composite_shadows WHERE name='current'").SingleAsync();
    }

    [Fact] void should_stamp_the_legacy_row_on_its_next_write() => _stamp.ShouldEqual("~k1:1:a1:b");
    [Fact] void should_never_rename_the_legacy_primary_key() => _id.ShouldEqual("a_b");
}
