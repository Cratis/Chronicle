// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_relation_has_the_wrong_shape : given.a_table_name_resolver
{
    void Establish()
    {
        _catalog.Names.Add("orders");
        _metadata.Claims.Add(new(_owner, Registered: true));
        _metadata.ShapeMatches = false;
    }

    async Task Because() => await Resolve();

    [Fact] void should_refuse_adoption() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_not_commit_a_binding() => _bindings.Rows.ShouldBeEmpty();
    [Fact] void should_leave_the_user_relation() => _catalog.Names.Single().ShouldEqual("orders");
}
