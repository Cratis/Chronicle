// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicIndexShape;

public class when_inspecting_filtered_sqlite_indexes : Specification
{
    Exception? _matching;
    Exception? _wrongFilter;
    Exception? _unfiltered;
    string _before;
    string _after;

    async Task Because()
    {
        await using var context = new DbContext(new DbContextOptionsBuilder().UseSqlite("Data Source=:memory:").WithoutServiceProviderCaching().Options);
        await context.Database.OpenConnectionAsync();
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE catalog_filter (Value INTEGER NOT NULL, Other INTEGER NULL)");
        await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX catalog_filter_value ON catalog_filter (Value) WHERE Other IS NOT NULL");
        _before = await Definition(context);
        _matching = await Inspect(context, "Other IS NOT NULL");
        _wrongFilter = await Inspect(context, "Other IS NULL");
        _unfiltered = await Inspect(context, null);
        _after = await Definition(context);
    }

    static Task<string> Definition(DbContext context) => context.Database.SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE name='catalog_filter_value'").SingleAsync();

    static async Task<Exception?> Inspect(DbContext context, string? filter)
    {
        using var inspection = DynamicSchemaVerification.Begin();
        await DynamicIndexShape.Verify(context, new() { Name = "catalog_filter_value", Table = "catalog_filter", Columns = ["Value"], IsUnique = true, Filter = filter });
        return Catch.Exception(() => DynamicSchemaVerification.Complete("test"));
    }

    [Fact] void should_accept_the_expected_filter() => _matching.ShouldBeNull();
    [Fact] void should_reject_a_different_filter() => _wrongFilter.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_not_accept_a_filtered_index_for_an_unfiltered_requirement() => _unfiltered.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_leave_the_index_definition_unchanged() => _after.ShouldEqual(_before);
}
