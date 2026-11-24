// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Json;

namespace Cratis.Chronicle.EventSequences.for_EventSerializer;

public class when_deserializing_revision_events : Specification
{
    AppendedEvent[] _stored;
    EventSerializer _serializer;
    EventRevised[] _internal;
    AppendedEvent[] _public;

    void Establish()
    {
        _stored = revised_header_events.All();
        var types = Substitute.For<IEventTypes>();
        types.GetClrTypeFor(typeof(EventRevised).GetEventType().Id).Returns(typeof(EventRevised));
        var schema = JsonSchema.FromJson("""{"type":"object"}""");
        types.GetJsonSchema(typeof(EventRevised)).Returns(schema);
        var converter = Substitute.For<IExpandoObjectConverter>();
        converter.ToJsonObject(Arg.Any<ExpandoObject>(), schema).Returns(call => JsonNode.Parse(JsonSerializer.Serialize(call.Arg<ExpandoObject>()))!.AsObject());
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new ConceptAsJsonConverterFactory() }
        };
        _serializer = new(types, converter, options);
    }

    void Because()
    {
        _public = _stored.Select(@event => PublicEventContentRedactor.Redact(@event, header_events.Options)).ToArray();
        _internal = _stored.Select(@event => (EventRevised)_serializer.Deserialize(@event)).ToArray();
    }

    [Fact] void should_keep_values_on_the_internal_deserialization_path() => _internal.All(@event => @event.Content.Contains("current-header-value", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_nested_type_for_internal_dispatch() => _internal.Select(@event => @event.EventType).ShouldEqual(header_events.Direct().Select(@event => @event.Context.EventType));
    [Fact] void should_not_keep_values_in_public_copies() => header_events.ContainsSentinel(JsonSerializer.Serialize(_public)).ShouldBeFalse();
    [Fact] void should_not_change_raw_values_after_a_public_read() => _stored.All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
}
