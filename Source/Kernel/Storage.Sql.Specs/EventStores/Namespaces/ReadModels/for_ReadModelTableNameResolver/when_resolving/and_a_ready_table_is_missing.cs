// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_ready_table_is_missing : given.a_table_name_resolver
{
    void Establish() => _bindings.Rows.Add(ReadModelTableNameBinding.For(_owner, "orders", ReadModelTableBindingState.Ready));

    async Task Because() => await Resolve();

    [Fact] void should_require_recovery_instead_of_recreating() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_leave_the_ready_binding_unchanged() => _bindings.Rows.Single().State.ShouldEqual(ReadModelTableBindingState.Ready);
    [Fact] void should_not_create_a_table() => _catalog.Names.ShouldBeEmpty();
}
