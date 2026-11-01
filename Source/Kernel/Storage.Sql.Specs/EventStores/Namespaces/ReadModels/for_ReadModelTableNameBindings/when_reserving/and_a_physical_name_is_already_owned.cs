// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_reserving;

public class and_a_physical_name_is_already_owned : given.a_sqlite_ledger
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    async Task should_enforce_unique_ownership_across_contexts_in_both_orders(bool differentStore, bool reverseOrder)
    {
        var other = differentStore ? _owner with { EventStoreId = "other-store" } : _owner with { Namespace = "other-namespace" };
        var first = reverseOrder ? other : _owner;
        var second = reverseOrder ? _owner : other;
        await Reserve(first, "orders");
        var independent = CreateLedger(CreateContext());
        var inserted = true;
        await independent.UnderLock(async () =>
        {
            var request = ReadModelTableNameBinding.For(second, "orders", ReadModelTableBindingState.Reserved);
            inserted = await independent.TryInsert(request);
            return request;
        });

        inserted.ShouldBeFalse();
        var rows = await independent.Read();
        rows.Count.ShouldEqual(1);
        rows.Single().Owner.ShouldEqual(first);
    }
}
