// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_possible_owner_is_found : given.a_table_name_resolver
{
    void Establish()
    {
        _catalog.Names.Add("orders");
        _metadata.Claims.Add(new(_owner with { EventStoreId = "other-store" }, Registered: true));
    }

    async Task Because() => await Resolve();

    [Fact] void should_refuse_the_other_owners_table() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_not_reserve_anything() => _bindings.Rows.ShouldBeEmpty();
    [Fact] void should_not_read_the_user_table() => _metadata.ShapeChecks.ShouldEqual(0);
}
