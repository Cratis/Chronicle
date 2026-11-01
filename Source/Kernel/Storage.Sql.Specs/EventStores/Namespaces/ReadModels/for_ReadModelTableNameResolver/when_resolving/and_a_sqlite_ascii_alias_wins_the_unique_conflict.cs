// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_sqlite_ascii_alias_wins_the_unique_conflict : given.a_table_name_resolver
{
    void Establish()
    {
        _catalog.Provider = DatabaseType.Sqlite;
        _bindings.BeforeInsert = _ => _bindings.Rows.Add(ReadModelTableNameBinding.For(
            _owner with { Namespace = "winning-namespace" }, "ORDERS", ReadModelTableBindingState.Reserved));
    }

    async Task Because() => await Resolve();

    [Fact] void should_refuse_the_lost_reservation() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_identify_the_winner_using_provider_equality() => _error.Message.Contains("winning-namespace", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_relabel_the_winning_name() => _bindings.Rows.Single().PhysicalName.ShouldEqual("ORDERS");
    [Fact] void should_not_create_a_user_table() => _catalog.Names.ShouldBeEmpty();
}
