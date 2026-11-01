// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_sqlite_names_alias_by_ascii_case : given.a_table_name_resolver
{
    void Establish() => _catalog.Provider = DatabaseType.Sqlite;

    [Theory]
    [InlineData("orders", "Orders")]
    [InlineData("Orders", "orders")]
    async Task should_reject_the_second_logical_owner_in_both_orders(string first, string second)
    {
        var firstOwner = _owner with { ContainerName = first, LogicalName = first };
        var secondOwner = _owner with { ContainerName = second, LogicalName = second };
        await CreateResolver().Resolve(firstOwner);
        var error = await Catch.Exception(() => CreateResolver().Resolve(secondOwner));

        firstOwner.ShouldNotEqual(secondOwner);
        firstOwner.Id.ShouldNotEqual(secondOwner.Id);
        error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
        _bindings.Rows.Single().Owner.ShouldEqual(firstOwner);
        _bindings.Rows.Single().PhysicalName.ShouldEqual(first);
        _metadata.ShapeChecks.ShouldEqual(0);
        _catalog.Names.ShouldBeEmpty();
    }
}
