// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_two_definitions_share_a_legacy_prefix : given.a_table_name_resolver
{
    void Establish()
    {
        var prefix = new string('x', 63);
        _owner = _owner with { ContainerName = prefix + "a", LogicalName = prefix + "a" };
        _metadata.Claims.Add(new(_owner, Registered: true));
        _metadata.Claims.Add(new(_owner with { ReadModelIdentifier = "other-model", ContainerName = prefix + "b", LogicalName = prefix + "b" }, Registered: true));
        _catalog.Names.Add(prefix);
    }

    async Task Because() => await Resolve();

    [Fact] void should_require_recovery() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_not_commit_a_binding() => _bindings.Rows.ShouldBeEmpty();
    [Fact] void should_leave_the_legacy_relation() => _catalog.Names.Count.ShouldEqual(1);
}
