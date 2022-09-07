// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.for_Projection;

public class when_next_event_is_a_public_event : given.a_projection
{
    static EventType public_event_type = new("05c2799e-e3ad-43b6-87bb-9fecb0b4e147", 1);
    AppendedEvent public_event;
    Mock<IChangeset<AppendedEvent, ExpandoObject>> changeset;
    List<ProjectionEventContext> observed_events;

    void Establish()
    {
        changeset = new();
        projection.SetEventTypesWithKeyResolvers(new EventTypeWithKeyResolver[]
        {
                new EventTypeWithKeyResolver(public_event_type, KeyResolvers.FromEventSourceId)
        });

        public_event = new(
            new(0, public_event_type),
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", "50308963-d8b5-4b6e-97c7-e2486e8237e1", "bfb7fd4a-1822-4937-a6d1-52464a173f84"),
            new JsonObject());

        observed_events = new();
        projection.Event.Subscribe(_ => observed_events.Add(_));
    }

    void Because() => projection.OnNext(new(new(public_event.Context.EventSourceId, ArrayIndexers.NoIndexers), public_event, changeset.Object));

    [Fact] void should_only_observe_one_event() => observed_events.Count.ShouldEqual(1);
    [Fact] void should_observe_the_public_event() => observed_events[0].Event.Metadata.Type.Id.ShouldEqual(public_event_type.Id);
}
