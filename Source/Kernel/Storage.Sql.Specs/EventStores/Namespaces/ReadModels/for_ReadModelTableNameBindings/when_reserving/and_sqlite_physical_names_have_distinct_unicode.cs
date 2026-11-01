// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_reserving;

public class and_sqlite_physical_names_have_distinct_unicode : given.a_sqlite_ledger
{
    [Theory]
    [InlineData("Ä", "ä")]
    [InlineData("K", "K")]
    [InlineData("S", "ſ")]
    [InlineData("Σ", "σ")]
    async Task should_keep_distinct_physical_owners(string first, string second)
    {
        await Reserve(_owner, first);
        var other = _owner with { Namespace = "other-namespace" };
        await Reserve(other, second);

        var rows = await CreateLedger(CreateContext()).Read();
        rows.Count.ShouldEqual(2);
        rows.Single(row => row.Owner == _owner).PhysicalName.ShouldEqual(first);
        rows.Single(row => row.Owner == other).PhysicalName.ShouldEqual(second);
    }
}
