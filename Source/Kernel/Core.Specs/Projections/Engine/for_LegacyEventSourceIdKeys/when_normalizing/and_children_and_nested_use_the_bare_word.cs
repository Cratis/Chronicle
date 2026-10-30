// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_children_and_nested_use_the_bare_word : given.a_projection_definition
{
    void Establish()
    {
        var payloadType = new EventType("payload-type", 1);
        _schemas = [Schema(_type, "name"), Schema(payloadType, "EventSourceId")];
        var grandchild = Child(from: new Dictionary<EventType, FromDefinition> { [_type] = From() });
        var child = Child(
            from: new Dictionary<EventType, FromDefinition> { [_type] = From(), [payloadType] = From() },
            join: new Dictionary<EventType, JoinDefinition> { [_type] = new("id", new Dictionary<PropertyPath, string>(), "EventSourceId") },
            removedWith: new Dictionary<EventType, RemovedWithDefinition> { [_type] = new("EventSourceId", "EventSourceId") },
            children: new Dictionary<PropertyPath, ChildrenDefinition> { ["grandchildren"] = grandchild },
            nested: new Dictionary<PropertyPath, ChildrenDefinition> { ["detail"] = grandchild });
        _definition = Definition(
            children: new Dictionary<PropertyPath, ChildrenDefinition> { ["children"] = child },
            nested: new Dictionary<PropertyPath, ChildrenDefinition> { ["detail"] = grandchild });
    }

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_rewrite_the_child_from_key() => _result.Children["children"].From[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_rewrite_the_child_join_key() => _result.Children["children"].Join[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_rewrite_the_child_removal_key() => _result.Children["children"].RemovedWith[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_rewrite_the_child_removal_parent_key() => _result.Children["children"].RemovedWith[_type].ParentKey!.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_keep_the_child_payload_key() => _result.Children["children"].From[new EventType("payload-type", 1)].Key.Value.ShouldEqual("EventSourceId");
    [Fact] void should_rewrite_the_grandchild_key() => _result.Children["children"].Children["grandchildren"].From[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_rewrite_the_nested_child_key() => _result.Children["children"].Nested!["detail"].From[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_rewrite_the_root_nested_key() => _result.Nested!["detail"].From[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_leave_the_original_child_key_unchanged() => _definition.Children["children"].From[_type].Key.Value.ShouldEqual("EventSourceId");
    [Fact] void should_preserve_the_identifying_property() => _result.Children["children"].IdentifiedBy.ShouldEqual(_definition.Children["children"].IdentifiedBy);
    [Fact] void should_preserve_the_every_event_mapping() => _result.Children["children"].All.ShouldBeSame(_definition.Children["children"].All);
    [Fact] void should_keep_both_views_of_the_every_event_mapping_consistent() => _result.Children["children"].FromEvery.ShouldBeSame(_result.Children["children"].All);
}
