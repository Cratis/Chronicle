// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using NJsonSchema;

namespace Aksio.Cratis.Events.Projections.for_Projection
{
    public class when_next_event_is_of_interest : Specification
    {
        static EventType event_a = new("05c2799e-e3ad-43b6-87bb-9fecb0b4e147", 1);
        static EventType event_b = new("4212376e-dd74-44f4-8ed4-1b7fe314d208", 1);
        Projection projection;
        List<EventContext> observed_events;
        ExpandoObject initial_state;

        Event first_event;
        Changeset<Event, ExpandoObject> first_changeset;

        Event second_event;
        Changeset<Event, ExpandoObject> second_changeset;

        void Establish()
        {
            projection = new Projection(
                "0b7325dd-7a25-4681-9ab7-c387a6073547",
                string.Empty,
                string.Empty,
                new Model(string.Empty, new JsonSchema()),
                new[] {
                    new EventTypeWithKeyResolver(event_b, EventValueProviders.FromEventSourceId)
                },
                Array.Empty<IProjection>());

            dynamic state = initial_state = new();
            state.Integer = 42;

            first_event = new Event(
                    0,
                    event_a,
                    DateTimeOffset.UtcNow,
                    "30c1ebf5-cc30-4216-afed-e3e0aefa1316",
                    new());

            first_changeset = new(first_event, new());

            second_event = new Event(
                    0,
                    event_b,
                    DateTimeOffset.UtcNow,
                    "30c1ebf5-cc30-4216-afed-e3e0aefa1316",
                    new());
            second_changeset = new(second_event, initial_state);

            observed_events = new();
            projection.Event.Subscribe(_ => observed_events.Add(_));
        }

        void Because()
        {
            projection.OnNext(first_event, first_changeset);
            projection.OnNext(second_event, second_changeset);
        }

        [Fact] void should_only_observe_one_event() => observed_events.Count.ShouldEqual(1);
        [Fact] void should_observe_the_event_of_interest() => observed_events[0].Event.Type.ShouldEqual(event_b);
        [Fact] void should_pass_along_the_initial_state() => observed_events[0].Changeset.InitialState.ShouldEqual(initial_state);
    }
}
