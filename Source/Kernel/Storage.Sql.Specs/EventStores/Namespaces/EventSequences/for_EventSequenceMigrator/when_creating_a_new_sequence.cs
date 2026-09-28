// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_creating_a_new_sequence : given.a_named_tag_migrator
{
    NamedTag _readBack;
    int _indexes;

    async Task Because()
    {
        await using var context = CreateContext();
        await context.EnsureTableExists();
        context.NamedTags.Add(NamedTagEntry.From("event-sequence", 0, 0, new(new TagName("n"), "value")));
        await context.SaveChangesAsync();
        _readBack = (await context.NamedTags.AsNoTracking().SingleAsync()).ToNamedTag();

        await using var command = _connection.CreateCommand();
#pragma warning disable CA2100 // Fixed provider-specific index-count queries.
        command.CommandText = _provider switch
        {
            "PostgreSQL" => "SELECT COUNT(*) FROM pg_indexes WHERE tablename='__cratis_named_tags'",
            "SQLServer" => "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID('__cratis_named_tags')",
            _ => "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND tbl_name='__cratis_named_tags'"
        };
#pragma warning restore CA2100
        _indexes = Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    [Fact] void should_create_a_writable_companion_table() => _readBack.Value.ShouldEqual("value");
    [Fact] void should_create_lookup_indexes() => _indexes.ShouldBeGreaterThan(2);
}
