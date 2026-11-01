// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_parent_keys_and_removals_use_the_bare_word : given.a_projection_definition
{
    void Establish() => _definition = Definition(
        from: new Dictionary<EventType, FromDefinition> { [_type] = From("id", "EventSourceId") },
        join: new Dictionary<EventType, JoinDefinition> { [_type] = new("id", new Dictionary<PropertyPath, string>(), "EventSourceId") },
        removedWith: new Dictionary<EventType, RemovedWithDefinition> { [_type] = new("EventSourceId", "EventSourceId") },
        removedWithJoin: new Dictionary<EventType, RemovedWithJoinDefinition> { [_type] = new("EventSourceId") });

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_rewrite_the_from_parent_key() => _result.From[_type].ParentKey!.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_keep_the_unrelated_from_key() => _result.From[_type].Key.Value.ShouldEqual("id");
    [Fact] void should_rewrite_the_join_key() => _result.Join[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_rewrite_the_removal_key() => _result.RemovedWith[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_rewrite_the_removal_parent_key() => _result.RemovedWith[_type].ParentKey!.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_rewrite_the_join_removal_key() => _result.RemovedWithJoin[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_leave_the_original_removal_key_unchanged() => _definition.RemovedWith[_type].Key.Value.ShouldEqual("EventSourceId");
}
