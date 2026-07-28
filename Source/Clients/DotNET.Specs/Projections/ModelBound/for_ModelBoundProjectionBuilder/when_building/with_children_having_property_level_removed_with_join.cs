// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class with_children_having_property_level_removed_with_join : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(ParentWithPropertyLevelJoinRemovableChildren));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_one_child_definition() => _result.Children.Count.ShouldEqual(1);
    [Fact] void should_have_child_with_correct_name() => _result.Children.ContainsKey(nameof(ParentWithPropertyLevelJoinRemovableChildren.Items)).ShouldBeTrue();
    [Fact] void should_not_have_removed_with_join_on_the_root() => _result.RemovedWithJoin.Count.ShouldEqual(0);
    [Fact] void should_have_removed_with_join_on_children() => _result.Children[nameof(ParentWithPropertyLevelJoinRemovableChildren.Items)].RemovedWithJoin.Count.ShouldEqual(1);
    [Fact] void should_have_removed_with_join_for_correct_event_type() => _result.Children[nameof(ParentWithPropertyLevelJoinRemovableChildren.Items)].RemovedWithJoin.Keys.First().Id.ShouldEqual(event_types.GetEventTypeFor(typeof(ChildItemRemovedJoin)).Id.ToString());
    [Fact] void should_use_specified_key_on_removed_with_join() => _result.Children[nameof(ParentWithPropertyLevelJoinRemovableChildren.Items)].RemovedWithJoin.Values.First().Key.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(ChildItemRemovedJoin.ItemId))));
}
