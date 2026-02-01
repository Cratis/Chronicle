// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Projections.for_EventValueProviders;

public class when_accessing_event_source_id : Specification
{
    AppendedEvent _event;
    object _result;

    void Establish() =>
            _event = new(
                new(
                    new("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
                    EventSourceType.Default,
                    "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                    EventStreamType.All,
                    EventStreamId.Default,
                    1,
                    DateTimeOffset.UtcNow,
                    "123b8935-a1a4-410d-aace-e340d48f0aa0",
                    "41f18595-4748-4b01-88f7-4c0d0907aa90",
                    CorrelationId.New(),
                    [],
                    Identity.System,
                    [],
                EventHash.NotSet),
                new ExpandoObject());

    void Because() => _result = EventValueProviders.EventSourceId(_event);

    [Fact] void should_return_the_guid_from_event_source_id_from_the_event() => _result.ShouldEqual(_event.Context.EventSourceId.Value);
}
