// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_an_independent_resolver_reopens_a_binding : given.a_table_name_resolver
{
    async Task Establish()
    {
        _metadata.Claims.Add(new(_owner, Registered: true));
        _catalog.Names.Add(_owner.LogicalName);
        await _resolver.Resolve(_owner);
        _resolver = CreateResolver();
    }

    async Task Because() => await Resolve(requireExisting: true);

    [Fact] void should_keep_the_binding() => _error.ShouldBeNull();
    [Fact] void should_not_duplicate_the_binding() => _bindings.Rows.Count.ShouldEqual(1);
    [Fact] void should_keep_the_table_name() => _result.PhysicalName.ShouldEqual(_owner.LogicalName);
    [Fact] void should_reuse_the_ready_assignment() => _result.State.ShouldEqual(ReadModelTableBindingState.Ready);
}
