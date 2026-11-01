// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_legacy_replay_state_is_unbound : given.a_table_name_resolver
{
    void Establish()
    {
        _owner = _owner with { Role = ReadModelTableRole.Replay, LogicalName = ReadModelTableNames.Replay(new string('x', 63)) };
        _catalog.Names.Add(ReadModelTableNames.Legacy(_catalog.Provider, _owner.LogicalName));
        _metadata.Claims.Add(new(_owner, Registered: true));
    }

    async Task Because() => await Resolve();

    [Fact] void should_not_silently_start_a_new_shadow() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_not_reserve_a_new_name() => _bindings.Rows.ShouldBeEmpty();
    [Fact] void should_preserve_interrupted_state() => _catalog.Names.Count.ShouldEqual(1);
    [Fact] void should_not_access_user_shape() => _metadata.ShapeChecks.ShouldEqual(0);
}
