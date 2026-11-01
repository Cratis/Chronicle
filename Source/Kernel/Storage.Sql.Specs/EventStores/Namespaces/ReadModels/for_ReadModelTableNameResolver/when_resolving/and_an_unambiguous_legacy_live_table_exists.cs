// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_an_unambiguous_legacy_live_table_exists : given.a_table_name_resolver
{
    string _legacy;

    void Establish()
    {
        var logical = new string('x', 70);
        _owner = _owner with { ContainerName = logical, LogicalName = logical };
        _legacy = new string('x', 63);
        _catalog.Names.Add(_legacy);
        _metadata.Claims.Add(new(_owner, Registered: true));
    }

    async Task Because() => await Resolve();

    [Fact] void should_succeed() => _error.ShouldBeNull();
    [Fact] void should_keep_the_old_physical_name() => _result.PhysicalName.ShouldEqual(_legacy);
    [Fact] void should_commit_a_ready_binding() => _result.State.ShouldEqual(ReadModelTableBindingState.Ready);
    [Fact] void should_not_create_an_empty_hashed_table() => _catalog.Names.ShouldEqual([_legacy]);
    [Fact] void should_persist_the_complete_owner() => _bindings.Rows.Single().Owner.ShouldEqual(_owner);
}
