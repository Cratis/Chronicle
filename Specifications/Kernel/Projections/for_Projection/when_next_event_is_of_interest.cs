// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Projections.for_Projection;

public class when_next_event_is_of_interest : given.a_projection
{
    static EventType event_a = new("05c2799e-e3ad-43b6-87bb-9fecb0b4e147", 1);
    static EventType event_b = new("4212376e-dd74-44f4-8ed4-1b7fe314d208", 1);
    List<ProjectionEventContext> observed_events;
    ExpandoObject initial_state;

    AppendedEvent first_event;
    Changeset<AppendedEvent, ExpandoObject> first_changeset;

    AppendedEvent second_event;
    Changeset<AppendedEvent, ExpandoObject> second_changeset;
    Mock<IObjectComparer> objects_comparer;

    void Establish()
    {
        projection.SetEventTypesWithKeyResolvers(
            new EventTypeWithKeyResolver[]
            {
                    new EventTypeWithKeyResolver(event_b, KeyResolvers.FromEventSourceId)
            },
            new[] { event_b });

        dynamic state = initial_state = new();
        state.Integer = 42;

        objects_comparer = new();
        objects_comparer.Setup(_ => _.Equals(IsAny<ExpandoObject>(), IsAny<AppendedEvent>(), out Ref<IEnumerable<PropertyDifference>>.IsAny)).Returns(true);

        first_event = new(
            new(0, event_a),
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", Enumerable.Empty<Causation>(), Identity.System),
            new ExpandoObject());

        first_changeset = new(objects_comparer.Object, first_event, new());

        second_event = new(
            new(0, event_b),
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", Enumerable.Empty<Causation>(), Identity.System),
            new ExpandoObject());

        second_changeset = new(objects_comparer.Object, second_event, initial_state);

        observed_events = new();
        projection.Event.Subscribe(observed_events.Add);
    }

    void Because()
    {
        projection.OnNext(new(new(first_event.Context.EventSourceId, ArrayIndexers.NoIndexers), first_event, first_changeset));
        projection.OnNext(new(new(second_event.Context.EventSourceId, ArrayIndexers.NoIndexers), second_event, second_changeset));
    }

    [Fact] void should_only_observe_one_event() => observed_events.Count.ShouldEqual(1);
    [Fact] void should_observe_the_event_of_interest() => observed_events[0].Event.Metadata.Type.ShouldEqual(event_b);
    [Fact] void should_pass_along_the_initial_state() => observed_events[0].Changeset.InitialState.ShouldEqual(initial_state);
}
