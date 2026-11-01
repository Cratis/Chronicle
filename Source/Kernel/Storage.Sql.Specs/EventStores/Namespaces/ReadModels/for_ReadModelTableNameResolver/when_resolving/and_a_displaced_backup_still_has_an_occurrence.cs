// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_displaced_backup_still_has_an_occurrence : given.a_table_name_resolver
{
    void Establish()
    {
        _owner = _owner with { Role = ReadModelTableRole.Backup, LogicalName = "orders-old" };
        _catalog.Names.Add("orders-old");
        _metadata.Claims.Add(new(_owner, Occurred: true, Displaced: true));
    }

    async Task Because() => await Resolve(requireExisting: true);

    [Fact] void should_not_reclaim_a_displaced_table() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_not_reserve_a_binding() => _bindings.Rows.ShouldBeEmpty();
    [Fact] void should_not_read_user_shape() => _metadata.ShapeChecks.ShouldEqual(0);
}
