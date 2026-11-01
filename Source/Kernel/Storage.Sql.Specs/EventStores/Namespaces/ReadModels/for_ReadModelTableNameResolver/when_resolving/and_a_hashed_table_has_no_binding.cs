// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_hashed_table_has_no_binding : given.a_table_name_resolver
{
    void Establish()
    {
        var logical = new string('x', 64);
        _owner = _owner with { ContainerName = logical, LogicalName = logical };
        _catalog.Names.Add(ReadModelTableNames.Candidate(_catalog.Provider, logical));
        _metadata.Claims.Add(new(_owner, Registered: true));
    }

    async Task Because() => await Resolve();

    [Fact] void should_not_guess_ownership_from_a_definition() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_not_probe_user_shape() => _metadata.ShapeChecks.ShouldEqual(0);
    [Fact] void should_not_commit_a_binding() => _bindings.Rows.ShouldBeEmpty();
}
