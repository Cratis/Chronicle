// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_an_internal_table_name_is_requested : given.a_table_name_resolver
{
    [Theory]
    [InlineData("chronicle_read_model_table_names")]
    [InlineData("chronicle_replay_promotions")]
    async Task should_refuse_without_user_data_access(string name)
    {
        _owner = _owner with { ContainerName = name, LogicalName = name };
        await Resolve();

        _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
        _bindings.Rows.ShouldBeEmpty();
        _metadata.ShapeChecks.ShouldEqual(0);
    }
}
