// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.given;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_events_reach_children_through_different_registrations : Specification
{
    const ProjectionOperationType FromOnChildren = ProjectionOperationType.From | ProjectionOperationType.ChildrenAffected;

    IProjection _root;
    IProjection _items;
    IProjection _subs;

    async Task Because()
    {
        _root = await ProjectionWithChildOnlyEvents.Create();
        _items = _root.ChildProjections.Single();
        _subs = _items.ChildProjections.Single();
    }

    [Fact] void should_make_a_root_from_a_root_operation() => _root.GetOperationTypeFor(ProjectionWithChildOnlyEvents.RootCreated).ShouldEqual(ProjectionOperationType.From);
    [Fact] void should_make_a_root_derivative_a_root_operation_even_when_a_child_handles_it_too() => _root.GetOperationTypeFor(ProjectionWithChildOnlyEvents.RootDerived).ShouldEqual(ProjectionOperationType.From);
    [Fact] void should_register_a_child_from_on_the_child() => _items.GetOperationTypeFor(ProjectionWithChildOnlyEvents.ItemAdded).ShouldEqual(ProjectionOperationType.From);
    [Fact] void should_affect_children_for_a_child_from() => _root.GetOperationTypeFor(ProjectionWithChildOnlyEvents.ItemAdded).ShouldEqual(FromOnChildren);
    [Fact] void should_register_a_child_event_property_on_the_child() => _items.GetOperationTypeFor(ProjectionWithChildOnlyEvents.ItemAttached).ShouldEqual(ProjectionOperationType.From);
    [Fact] void should_affect_children_for_a_child_event_property() => _root.GetOperationTypeFor(ProjectionWithChildOnlyEvents.ItemAttached).ShouldEqual(FromOnChildren);
    [Fact] void should_register_a_nested_object_event_on_the_child() => _items.GetOperationTypeFor(ProjectionWithChildOnlyEvents.DetailSet).ShouldEqual(ProjectionOperationType.From);
    [Fact] void should_affect_children_for_a_nested_object_event_in_a_child() => _root.GetOperationTypeFor(ProjectionWithChildOnlyEvents.DetailSet).ShouldEqual(FromOnChildren);
    [Fact] void should_register_a_grandchild_from_on_the_grandchild() => _subs.GetOperationTypeFor(ProjectionWithChildOnlyEvents.SubAdded).ShouldEqual(ProjectionOperationType.From);
    [Fact] void should_register_a_grandchild_event_property_on_the_grandchild() => _subs.GetOperationTypeFor(ProjectionWithChildOnlyEvents.SubAttached).ShouldEqual(ProjectionOperationType.From);
    [Fact] void should_affect_children_of_the_child_for_a_grandchild_event_property() => _items.GetOperationTypeFor(ProjectionWithChildOnlyEvents.SubAttached).ShouldEqual(FromOnChildren);
    [Fact] void should_affect_children_of_the_root_for_a_grandchild_event_property() => _root.GetOperationTypeFor(ProjectionWithChildOnlyEvents.SubAttached).ShouldEqual(FromOnChildren);
    [Fact] void should_not_register_an_unrelated_event_on_a_child_with_a_from_every() => _items.GetOperationTypeFor(ProjectionWithChildOnlyEvents.Unrelated).ShouldEqual(ProjectionOperationType.None);
    [Fact] void should_not_register_an_unrelated_event_on_the_root() => _root.GetOperationTypeFor(ProjectionWithChildOnlyEvents.Unrelated).ShouldEqual(ProjectionOperationType.None);
}
