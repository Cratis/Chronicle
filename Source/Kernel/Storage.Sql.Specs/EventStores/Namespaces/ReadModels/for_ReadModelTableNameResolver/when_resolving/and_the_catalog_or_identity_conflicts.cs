// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_the_catalog_or_identity_conflicts : given.a_table_name_resolver
{
    [Theory]
    [InlineData("store")]
    [InlineData("namespace")]
    [InlineData("model")]
    [InlineData("container")]
    [InlineData("role")]
    [InlineData("logical")]
    async Task should_validate_every_full_field_even_when_the_digest_matches(string field)
    {
        var row = ReadModelTableNameBinding.For(_owner, "orders", ReadModelTableBindingState.Ready);
        switch (field)
        {
            case "store": row.EventStoreId = "other"; break;
            case "namespace": row.Namespace = "other"; break;
            case "model": row.ReadModelIdentifier = "other"; break;
            case "container": row.ContainerName = "other"; break;
            case "role": row.Role = ReadModelTableRole.Backup; break;
            case "logical": row.LogicalName = "other"; break;
        }
        _bindings.Rows.Add(row);
        _catalog.Names.Add("orders");
        await Resolve();

        _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
        _metadata.ShapeChecks.ShouldEqual(0);
        _bindings.Rows.Count.ShouldEqual(1);
    }
}
