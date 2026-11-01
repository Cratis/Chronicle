// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_possible_owner_is_unknown : given.a_table_name_resolver
{
    void Establish()
    {
        _catalog.Names.Add("orders");
        _metadata.UnknownScopes.Add(ReadModelTableMetadataScope.Unknown("other-store", null, "visibility", "catalog visibility not established"));
    }

    async Task Because() => await Resolve();

    [Fact] void should_fail_closed_with_a_named_error() => _error.ShouldBeOfExactType<ReadModelTableMetadataUnavailable>();
    [Fact] void should_name_the_unknown_scope() => _error.Message.ShouldContain("other-store");
    [Fact] void should_not_reserve_or_adopt() => _bindings.Rows.ShouldBeEmpty();
    [Fact] void should_not_read_the_user_table() => _metadata.ShapeChecks.ShouldEqual(0);
    [Fact] void should_leave_the_catalog_unchanged() => _catalog.Names.ShouldContainOnly("orders");
}
