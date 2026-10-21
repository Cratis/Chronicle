// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_child_joins;

public class with_a_nested_object : given.a_child_join_builder
{
    ChildrenDefinition _nested;

    void Because() => _nested = _builder.Build(typeof(OrderWithNestedProduct)).Nested["product"];

    [Fact] void should_not_have_a_child_identity() => _nested.IdentifiedBy.ShouldEqual(PropertyPath.NotSet.Path);
    [Fact] void should_join_on_the_serialized_property_name() => _nested.Join.Values.Single().On.ShouldEqual("productName");
    [Fact] void should_join_by_event_source_id() => _nested.Join.Values.Single().Key.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_map_only_the_joined_property() => _nested.Join.Values.Single().Properties.ShouldContainOnly(new KeyValuePair<string, string>("productName", "name"));
}
