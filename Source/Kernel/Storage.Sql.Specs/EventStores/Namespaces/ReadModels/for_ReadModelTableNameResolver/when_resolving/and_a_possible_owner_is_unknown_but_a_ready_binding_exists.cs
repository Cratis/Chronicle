// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_possible_owner_is_unknown_but_a_ready_binding_exists : given.a_table_name_resolver
{
    void Establish()
    {
        _bindings.Rows.Add(ReadModelTableNameBinding.For(_owner, "orders", ReadModelTableBindingState.Ready));
        _catalog.Names.Add("orders");
        _metadata.UnknownScopes.Add(ReadModelTableMetadataScope.Unknown("other-store", null, "visibility", "catalog visibility not established"));
    }

    async Task Because() => await Resolve();

    [Fact] void should_keep_serving_the_ready_table() => _error.ShouldBeNull();
    [Fact] void should_return_the_binding() => _result.PhysicalName.ShouldEqual("orders");
    [Fact] void should_not_scan_ownership_on_a_ledger_hit() => _metadata.Reads.ShouldEqual(0);
}
