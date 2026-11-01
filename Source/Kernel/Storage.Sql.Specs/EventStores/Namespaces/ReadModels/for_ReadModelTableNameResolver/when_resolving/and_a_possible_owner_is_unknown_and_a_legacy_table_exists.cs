// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_possible_owner_is_unknown_and_a_legacy_table_exists : given.a_table_name_resolver
{
    void Establish()
    {
        _catalog.Names.Add("orders");
        _metadata.Claims.Add(new(_owner, Registered: true));
        _metadata.UnknownScopes.Add(ReadModelTableMetadataScope.Unknown("other-store", null, "visibility", "catalog visibility not established"));
    }

    async Task Because() => await Resolve();

    [Fact] void should_keep_adopting_the_existing_relation_as_before() => _error.ShouldBeNull();
    [Fact] void should_keep_the_existing_physical_name() => _result.PhysicalName.ShouldEqual("orders");
    [Fact] void should_commit_a_ready_binding() => _result.State.ShouldEqual(ReadModelTableBindingState.Ready);
    [Fact] void should_not_create_a_table() => _catalog.Names.ShouldContainOnly("orders");
}
