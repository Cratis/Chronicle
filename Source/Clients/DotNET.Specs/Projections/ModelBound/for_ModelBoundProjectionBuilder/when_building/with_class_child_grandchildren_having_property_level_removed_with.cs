// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class with_class_child_grandchildren_having_property_level_removed_with : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;
    ChildrenDefinition _itemsDef;
    ChildrenDefinition _subItemsDef;

    void Because()
    {
        _result = builder.Build(typeof(RootWithClassChildGrandchildren));
        _result.Children.TryGetValue(nameof(RootWithClassChildGrandchildren.Items), out _itemsDef);
        _itemsDef?.Children.TryGetValue(nameof(ClassChildWithRemovableGrandchildren.SubItems), out _subItemsDef);
    }

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_the_items_child() => _itemsDef.ShouldNotBeNull();
    [Fact] void should_have_the_subitems_grandchild() => _subItemsDef.ShouldNotBeNull();
    [Fact] void should_not_have_removed_with_on_the_root() => _result.RemovedWith.Count.ShouldEqual(0);
    [Fact] void should_not_have_removed_with_on_the_parent_item() => _itemsDef.RemovedWith.Count.ShouldEqual(0);
    [Fact] void should_have_removed_with_on_the_grandchild() => _subItemsDef.RemovedWith.Count.ShouldEqual(1);
    [Fact] void should_have_removed_with_for_correct_event_type() => _subItemsDef.RemovedWith.Keys.First().Id.ShouldEqual(event_types.GetEventTypeFor(typeof(SubItemRemovedFromItem)).Id.ToString());
    [Fact] void should_use_specified_key_on_removed_with() => _subItemsDef.RemovedWith.Values.First().Key.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(SubItemRemovedFromItem.SubItemId))));
}
