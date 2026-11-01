// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_logical_scopes_share_one_placement : given.a_table_name_resolver
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    async Task should_reject_the_other_scoped_owner_in_both_orders(bool differentStore, bool reverseOrder)
    {
        var other = differentStore ? _owner with { EventStoreId = "another-store" } : _owner with { Namespace = "another-namespace" };
        var first = reverseOrder ? other : _owner;
        var second = reverseOrder ? _owner : other;
        await CreateResolver().Resolve(first);
        var error = await Catch.Exception(() => CreateResolver().Resolve(second));

        first.Id.ShouldNotEqual(second.Id);
        error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
        _bindings.Rows.Single().Owner.ShouldEqual(first);
        _catalog.Names.ShouldBeEmpty();
        _metadata.ShapeChecks.ShouldEqual(0);
    }
}
