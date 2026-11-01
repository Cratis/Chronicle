// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_ready_binding_is_validated : given.a_table_name_resolver
{
    void Establish()
    {
        _bindings.Rows.Add(ReadModelTableNameBinding.For(_owner, "orders", ReadModelTableBindingState.Ready));
        _catalog.Names.Add("orders");
    }

    async Task Because() => await Resolve();

    [Fact] void should_resolve() => _error.ShouldBeNull();
    [Fact] void should_not_read_ownership_metadata() => _metadata.Reads.ShouldEqual(0);
    [Fact] void should_not_check_the_shape() => _metadata.ShapeChecks.ShouldEqual(0);
}
