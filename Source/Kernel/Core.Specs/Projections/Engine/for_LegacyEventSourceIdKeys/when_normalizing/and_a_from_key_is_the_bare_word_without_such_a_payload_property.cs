// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_a_from_key_is_the_bare_word_without_such_a_payload_property : given.a_projection_definition
{
    void Establish() => _definition = Definition(from: new Dictionary<EventType, FromDefinition> { [_type] = From() });

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_use_the_event_source_id() => _result.From[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_leave_the_stored_definition_unchanged() => _definition.From[_type].Key.Value.ShouldEqual("EventSourceId");
    [Fact] void should_report_the_rewritten_type() => _rewritten.ShouldContainOnly(_type);
}
