// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_an_occurrence_table_is_missing : given.a_table_name_resolver
{
    void Establish()
    {
        _owner = _owner with { Role = ReadModelTableRole.Backup, LogicalName = "orders-20260607120000-12345678" };
        _metadata.Claims.Add(new(_owner, Occurred: true));
    }

    async Task Because() => await Resolve(requireExisting: true);

    [Fact] void should_require_recovery() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_not_reserve_or_create_a_replacement() => _bindings.Rows.ShouldBeEmpty();
    [Fact] void should_not_create_a_user_table() => _catalog.Names.ShouldBeEmpty();
}
