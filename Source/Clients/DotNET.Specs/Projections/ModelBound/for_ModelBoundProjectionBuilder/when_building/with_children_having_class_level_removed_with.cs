// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class with_children_having_class_level_removed_with : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(ParentWithRemovableChildren));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_one_child_definition() => _result.Children.Count.ShouldEqual(1);
    [Fact] void should_have_child_with_correct_name() => _result.Children.ContainsKey(nameof(ParentWithRemovableChildren.Items)).ShouldBeTrue();
    [Fact] void should_have_removed_with_on_children() => _result.Children[nameof(ParentWithRemovableChildren.Items)].RemovedWith.Count.ShouldEqual(1);
    [Fact] void should_have_removed_with_for_correct_event_type() => _result.Children[nameof(ParentWithRemovableChildren.Items)].RemovedWith.Keys.First().Id.ShouldEqual(event_types.GetEventTypeFor(typeof(ChildItemRemoved)).Id.ToString());
    [Fact] void should_use_specified_key_on_removed_with() => _result.Children[nameof(ParentWithRemovableChildren.Items)].RemovedWith.Values.First().Key.ShouldEqual(nameof(ChildItemRemoved.ItemId));
    [Fact] void should_use_specified_parent_key_on_removed_with() => _result.Children[nameof(ParentWithRemovableChildren.Items)].RemovedWith.Values.First().ParentKey.ShouldEqual(nameof(ChildItemRemoved.ParentId));
}
