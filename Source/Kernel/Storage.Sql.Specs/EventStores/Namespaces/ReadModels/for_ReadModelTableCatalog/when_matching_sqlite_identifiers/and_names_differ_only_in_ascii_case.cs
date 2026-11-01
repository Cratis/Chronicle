// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableCatalog.when_matching_sqlite_identifiers;

public class and_names_differ_only_in_ascii_case : for_ReadModelTableNameBindings.given.a_sqlite_ledger
{
    string _storedName;
    bool _equal;

    async Task Establish() => await _context.Database.ExecuteSqlRawAsync("CREATE TABLE Orders (Id TEXT NOT NULL PRIMARY KEY)");

    async Task Because()
    {
        var catalog = new ReadModelTableCatalog(_context, "main");
        _equal = await catalog.Equal("Orders", "orders");
        _storedName = await catalog.Find("oRDERs");
    }

    [Fact] void should_match_native_sqlite_identifier_equality() => _equal.ShouldBeTrue();
    [Fact] void should_discover_the_relation_with_its_actual_spelling() => _storedName.ShouldEqual("Orders");
}
