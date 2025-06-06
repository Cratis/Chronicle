// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_EventValueProviders;

public class when_getting_event_source_id : Specification
{
    static EventSourceId eventSourceId = "2f005aaf-2f4e-4a47-92ea-63687ef74bd4";

    ValueProvider<AppendedEvent> value_provider;
    AppendedEvent @event;
    object result;

    void Establish()
    {
        @event = new(
            new(1,
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
            new ExpandoObject());
        value_provider = EventValueProviders.EventSourceId;
    }

    void Because() => result = value_provider(@event).ToString();

    [Fact] void should_return_the_event_source_id() => result.ShouldEqual(eventSourceId.ToString());
}
