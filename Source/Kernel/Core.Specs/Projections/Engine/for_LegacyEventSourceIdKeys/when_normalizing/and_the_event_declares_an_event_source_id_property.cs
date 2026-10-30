// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_the_event_declares_an_event_source_id_property : given.a_projection_definition
{
    void Establish()
    {
        _schemas = [Schema(_type, "EventSourceId")];
        _definition = Definition(from: new Dictionary<EventType, FromDefinition> { [_type] = From() });
    }

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_keep_the_payload_key() => _result.From[_type].Key.Value.ShouldEqual("EventSourceId");
    [Fact] void should_not_report_a_rewrite() => _rewritten.ShouldBeEmpty();
}
