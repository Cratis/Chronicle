// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_backup_has_an_explicit_physical_owner : given.a_table_name_resolver
{
    void Establish()
    {
        _owner = _owner with { Role = ReadModelTableRole.Backup, LogicalName = "orders-20260607120000-12345678" };
        _catalog.Names.Add("historical-backup");
        _metadata.Claims.Add(new(_owner, Occurred: true, PromotedPhysicalName: "historical-backup"));
    }

    async Task Because() => await Resolve(requireExisting: true);

    [Fact] void should_succeed() => _error.ShouldBeNull();
    [Fact] void should_use_the_marker_owner_not_the_candidate() => _result.PhysicalName.ShouldEqual("historical-backup");
    [Fact] void should_persist_the_original_identity() => _bindings.Rows.Single().Owner.ShouldEqual(_owner);
}
