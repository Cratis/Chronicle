// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_another_owner_wins_a_unique_conflict : given.a_table_name_resolver
{
    void Establish() => _bindings.BeforeInsert = binding =>
        _bindings.Rows.Add(ReadModelTableNameBinding.For(_owner with { Namespace = "winner" }, binding.PhysicalName, ReadModelTableBindingState.Reserved));

    async Task Because() => await Resolve();

    [Fact] void should_refuse_the_lost_reservation() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_not_move_the_winner() => _bindings.Rows.Single().Namespace.ShouldEqual("winner");
    [Fact] void should_not_create_a_user_table() => _catalog.Names.ShouldBeEmpty();
}
