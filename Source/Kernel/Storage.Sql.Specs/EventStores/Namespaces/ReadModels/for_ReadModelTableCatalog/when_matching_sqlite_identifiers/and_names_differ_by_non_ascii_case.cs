// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableCatalog.when_matching_sqlite_identifiers;

public class and_names_differ_by_non_ascii_case : for_ReadModelTableNameBindings.given.a_sqlite_ledger
{
    async Task Establish() => await _context.Database.ExecuteSqlRawAsync("""
        CREATE TABLE "Ä" (Id TEXT);
        CREATE TABLE "ä" (Id TEXT);
        CREATE TABLE "K" (Id TEXT);
        CREATE TABLE "K" (Id TEXT);
        CREATE TABLE "S" (Id TEXT);
        CREATE TABLE "ſ" (Id TEXT);
        CREATE TABLE "Σ" (Id TEXT);
        CREATE TABLE "σ" (Id TEXT);
        """);

    [Theory]
    [InlineData("Ä", "ä")]
    [InlineData("K", "K")]
    [InlineData("S", "ſ")]
    [InlineData("Σ", "σ")]
    async Task should_preserve_native_unicode_distinctions(string first, string second)
    {
        var catalog = new ReadModelTableCatalog(_context, "main");
        (await catalog.Equal(first, second)).ShouldBeFalse();
        (await catalog.Find(first)).ShouldEqual(first);
        (await catalog.Find(second)).ShouldEqual(second);
    }

    [Theory]
    [InlineData("Orders", "oRDERS", true)]
    [InlineData("Ä", "ä", false)]
    [InlineData("K", "K", false)]
    [InlineData("S", "ſ", false)]
    [InlineData("Σ", "σ", false)]
    [InlineData("ÄA", "Äa", true)]
    [InlineData("orders", "order", false)]
    async Task should_apply_only_ascii_folding_in_memory(string first, string second, bool equal)
    {
        var catalog = new ReadModelTableCatalog(_context, "main");
        await _context.Database.CloseConnectionAsync();
        (await catalog.Equal(first, second)).ShouldEqual(equal);
        _context.Database.GetDbConnection().State.ShouldEqual(System.Data.ConnectionState.Closed);
    }
}
