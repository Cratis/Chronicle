// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_both_candidate_and_legacy_relations_exist : given.a_table_name_resolver
{
    void Establish()
    {
        var logical = new string('x', 64);
        _owner = _owner with { ContainerName = logical, LogicalName = logical };
        _metadata.Claims.Add(new(_owner, Registered: true));
        _catalog.Names.Add(ReadModelTableNames.Candidate(_catalog.Provider, logical));
        _catalog.Names.Add(ReadModelTableNames.Legacy(_catalog.Provider, logical));
    }

    async Task Because() => await Resolve();

    [Fact] void should_not_choose_one_by_its_contents() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_not_commit_a_binding() => _bindings.Rows.ShouldBeEmpty();
    [Fact] void should_not_access_user_shape() => _metadata.ShapeChecks.ShouldEqual(0);
    [Fact] void should_leave_both_relations_unchanged() => _catalog.Names.Count.ShouldEqual(2);
}
