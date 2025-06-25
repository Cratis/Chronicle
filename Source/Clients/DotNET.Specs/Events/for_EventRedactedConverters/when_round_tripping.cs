// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle.Events.for_EventRedactedConverters;

public class when_round_tripping : Specification
{
    const string eventTypeId = "some-event-type-id";
    EventRedacted _original;
    EventRedacted _result;
    IEventTypes _eventTypes;
    JsonSerializerOptions _options;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        var eventType = typeof(string); // Use string as a dummy type
        _eventTypes.GetClrTypeFor(eventTypeId).Returns(eventType);
        _eventTypes.GetEventTypeFor(eventType).Returns(new EventType(eventTypeId, EventTypeGeneration.First));
        _options = new JsonSerializerOptions { Converters = { new EventRedactedConverters(_eventTypes) } };
        var properties = new Dictionary<string, string> { { "key", "value" } };
        _original = new EventRedacted(
            "reason",
            eventType,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            [new Causation(DateTimeOffset.UtcNow, "TheCausationType", properties)],
            [Guid.NewGuid()]);
    }

    void Because()
    {
        var json = JsonSerializer.Serialize(_original, _options);
        _result = JsonSerializer.Deserialize<EventRedacted>(json, _options)!;
    }

    [Fact] void should_preserve_reason() => _result.Reason.ShouldEqual(_original.Reason);
    [Fact] void should_preserve_original_event_type() => _result.OriginalEventType.ShouldEqual(_original.OriginalEventType);
    [Fact] void should_preserve_occurred() => _result.Occurred.ShouldEqual(_original.Occurred);
    [Fact] void should_preserve_correlation_id() => _result.CorrelationId.ShouldEqual(_original.CorrelationId);
    [Fact] void should_preserve_causation_occurred() => _result.Causation.First().Occurred.ShouldEqual(_original.Causation.First().Occurred);
    [Fact] void should_preserve_causation_type() => _result.Causation.First().Type.ShouldEqual(_original.Causation.First().Type);
    [Fact] void should_preserve_causation_properties() => _result.Causation.First().Properties.Values.ShouldEqual(_original.Causation.First().Properties.Values);
    [Fact] void should_preserve_caused_by() => _result.CausedBy.ShouldEqual(_original.CausedBy);
}
