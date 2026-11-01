// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_reserving;

public class and_sqlite_physical_names_differ_only_by_ascii_case : given.a_sqlite_ledger
{
    [Theory]
    [InlineData("orders", "Orders")]
    [InlineData("Orders", "orders")]
    async Task should_keep_only_the_first_owner_in_independent_contexts(string first, string second)
    {
        await Reserve(_owner, first);
        var other = _owner with { Namespace = "other-namespace" };
        var independent = CreateLedger(CreateContext());
        var inserted = true;
        await independent.UnderLock(async () =>
        {
            var row = ReadModelTableNameBinding.For(other, second, ReadModelTableBindingState.Reserved);
            inserted = await independent.TryInsert(row);
            return row;
        });

        inserted.ShouldBeFalse();
        var rows = await independent.Read();
        rows.Count.ShouldEqual(1);
        rows.Single().Owner.ShouldEqual(_owner);
        rows.Single().PhysicalName.ShouldEqual(first);
        rows.Single().State.ShouldEqual(ReadModelTableBindingState.Reserved);
        _owner.ShouldNotEqual(other);
    }
}
