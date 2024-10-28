// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_EventValueProviders;

public class when_getting_boolean_true_value_from_event_content : Specification
{
    const bool expected = true;
    ValueProvider<AppendedEvent> value_provider;
    AppendedEvent @event;
    object result;

    void Establish()
    {
        var content = new ExpandoObject();
        ((dynamic)content).propertySet = expected;

        @event = new(
            new(0,
            new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)),
            new(
                EventSourceType.Default,
                "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System),
            content);
        value_provider = EventValueProviders.EventContent("PropertySet");
    }

    void Because() => result = value_provider(@event);

    [Fact] void should_return_content_of_source_property() => result.ShouldEqual(expected);
}
