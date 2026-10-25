// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_an_inherited_payload_property_declares_the_key : given.a_projection_definition
{
    static readonly EventType _oneOfType = new("one-of-type", 1);

    void Establish()
    {
        _schemas = [new EventTypeSchema(_type, EventTypeOwner.Client, EventTypeSource.User, JsonSchema.FromJson("""
            {
              "type": "object",
              "allOf": [
                { "type": "object", "properties": { "name": { "type": "string" } } },
                { "type": "object", "properties": { "EventSourceId": { "type": "string" } } }
              ]
            }
            """)),
            new EventTypeSchema(_otherType, EventTypeOwner.Client, EventTypeSource.User, JsonSchema.FromJson("""
                { "anyOf": [
                  { "type": "object", "properties": { "name": { "type": "string" } } },
                  { "type": "object", "properties": { "EventSourceId": { "type": "string" } } }
                ] }
                """)),
            new EventTypeSchema(_oneOfType, EventTypeOwner.Client, EventTypeSource.User, JsonSchema.FromJson("""
                { "oneOf": [
                  { "type": "object", "properties": { "name": { "type": "string" } } },
                  { "type": "object", "properties": { "EventSourceId": { "type": "string" } } }
                ] }
                """))];
        _definition = Definition(from: new Dictionary<EventType, FromDefinition> { [_type] = From(), [_otherType] = From(), [_oneOfType] = From() });
    }

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_keep_the_inherited_payload_key() => _result.From[_type].Key.Value.ShouldEqual("EventSourceId");
    [Fact] void should_keep_a_payload_key_in_another_any_of_branch() => _result.From[_otherType].Key.Value.ShouldEqual("EventSourceId");
    [Fact] void should_keep_a_payload_key_in_another_one_of_branch() => _result.From[_oneOfType].Key.Value.ShouldEqual("EventSourceId");
    [Fact] void should_not_report_a_rewrite() => _rewritten.ShouldBeEmpty();
}
