// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_nested_child_paths_are_camel_case_with_a_pascal_case_schema : given.a_child_collection_projection
{
    IProjection _projection;

    void Establish() => _definition.Children["notes"].Children["replies"] = Child();

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);

    [Fact] void should_create_the_nested_child_projection() => _projection.ChildProjections.Single().ChildProjections.Count().ShouldEqual(1);
    [Fact] void should_keep_the_nested_array_path() => _projection.ChildProjections.Single().ChildProjections.Single().ChildrenPropertyPath.ShouldEqual(new PropertyPath("[notes].[replies]"));
    [Fact] void should_resolve_the_nested_item_schema() => _projection.ChildProjections.Single().ChildProjections.Single().TargetReadModelSchema.Properties["Id"].Type.ShouldEqual(JsonObjectType.String);
}
