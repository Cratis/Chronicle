// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_metadata_is_unavailable : given.a_table_name_resolver
{
    void Establish() => _metadata.Failure = new InvalidOperationException("Metadata unavailable");

    async Task Because() => await Resolve();

    [Fact] void should_propagate_the_failure() => _error.ShouldEqual(_metadata.Failure);
    [Fact] void should_not_assume_no_competitors() => _bindings.Rows.ShouldBeEmpty();
    [Fact] void should_not_access_user_shape() => _metadata.ShapeChecks.ShouldEqual(0);
}
