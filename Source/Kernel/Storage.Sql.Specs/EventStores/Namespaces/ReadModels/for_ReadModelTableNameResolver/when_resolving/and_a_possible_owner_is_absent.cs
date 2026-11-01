// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_possible_owner_is_absent : given.a_table_name_resolver
{
    async Task Because() => await Resolve();

    [Fact] void should_reserve_the_name() => _result.State.ShouldEqual(ReadModelTableBindingState.Reserved);
    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_commit_one_reservation() => _bindings.Rows.Count.ShouldEqual(1);
}
