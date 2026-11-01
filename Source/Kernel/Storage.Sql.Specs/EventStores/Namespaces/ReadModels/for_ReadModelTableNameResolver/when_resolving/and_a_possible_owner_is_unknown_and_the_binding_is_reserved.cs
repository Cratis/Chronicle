// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_possible_owner_is_unknown_and_the_binding_is_reserved : given.a_table_name_resolver
{
    void Establish()
    {
        _bindings.Rows.Add(ReadModelTableNameBinding.For(_owner, "orders", ReadModelTableBindingState.Reserved));
        _metadata.UnknownScopes.Add(ReadModelTableMetadataScope.Unknown("*", null, "registry", "InvalidOperationException"));
    }

    async Task Because() => await Resolve();

    [Fact] void should_not_finish_the_reservation() => _error.ShouldBeOfExactType<ReadModelTableMetadataUnavailable>();
    [Fact] void should_keep_the_reservation_unchanged() => _bindings.Rows.Single().State.ShouldEqual(ReadModelTableBindingState.Reserved);
}
