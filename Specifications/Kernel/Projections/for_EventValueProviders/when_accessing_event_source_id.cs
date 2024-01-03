// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Projections.for_EventValueProviders;

public class when_accessing_event_source_id : Specification
{
    AppendedEvent @event;
    object result;

    void Establish() =>
            @event = new(
                new(0,
                new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)),
                new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", Enumerable.Empty<Causation>(), Identity.System),
                new ExpandoObject());

    void Because() => result = EventValueProviders.EventSourceId(@event);

    [Fact] void should_return_the_guid_from_event_source_id_from_the_event() => result.ShouldEqual(@event.Context.EventSourceId.Value);
}
