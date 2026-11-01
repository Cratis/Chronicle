// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_the_same_owner_requests_twice : given.a_table_name_resolver
{
    async Task Establish() => await _resolver.Resolve(_owner);

    async Task Because() => await Resolve();

    [Fact] void should_allow_the_owner_to_finish_provisioning() => _error.ShouldBeNull();
    [Fact] void should_keep_one_reservation() => _bindings.Rows.Count.ShouldEqual(1);
    [Fact] void should_remain_reserved_until_provisioned() => _result.State.ShouldEqual(ReadModelTableBindingState.Reserved);
    [Fact] void should_not_provision_a_user_table() => _catalog.Names.ShouldBeEmpty();
}
