// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_Projection;

public class when_next_event_is_of_interest : given.a_projection
{
    static EventType _eventA = new("05c2799e-e3ad-43b6-87bb-9fecb0b4e147", 1);
    static EventType _eventB = new("4212376e-dd74-44f4-8ed4-1b7fe314d208", 1);
    List<ProjectionEventContext> _observedEvents;
    ExpandoObject _initialState;

    AppendedEvent _firstEvent;
    Changeset<AppendedEvent, ExpandoObject> _firstChangeset;

    AppendedEvent _secondEvent;
    Changeset<AppendedEvent, ExpandoObject> _secondChangeset;
    IObjectComparer _objectsComparer;

    void Establish()
    {
        projection.SetEventTypesWithKeyResolvers(
            [
                    new EventTypeWithKeyResolver(_eventB, keyResolvers.FromEventSourceId)
            ],
            [_eventB],
            new Dictionary<EventType, ProjectionOperationType>());

        dynamic state = _initialState = new();
        state.Integer = 42;

        _objectsComparer = Substitute.For<IObjectComparer>();
        _objectsComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<AppendedEvent>(), out Arg.Any<IEnumerable<PropertyDifference>>()).Returns(true);

        _firstEvent = new(
            new(
                _eventA,
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
                Identity.System,
                []),
            new ExpandoObject());

        _firstChangeset = new(_objectsComparer, _firstEvent, new());

        _secondEvent = new(
            new(
                _eventB,
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
                Identity.System,
                []),
            new ExpandoObject());

        _secondChangeset = new(_objectsComparer, _secondEvent, _initialState);

        _observedEvents = [];
        projection.Event.Subscribe(_observedEvents.Add);
    }

    void Because()
    {
        projection.OnNext(new(new(_firstEvent.Context.EventSourceId, ArrayIndexers.NoIndexers), _firstEvent, _firstChangeset, ProjectionOperationType.From, false));
        projection.OnNext(new(new(_secondEvent.Context.EventSourceId, ArrayIndexers.NoIndexers), _secondEvent, _secondChangeset, ProjectionOperationType.From, false));
    }

    [Fact] void should_only_observe_one_event() => _observedEvents.Count.ShouldEqual(1);
    [Fact] void should_observe_the_event_of_interest() => _observedEvents[0].Event.Context.EventType.ShouldEqual(_eventB);
    [Fact] void should_pass_along_the_initial_state() => _observedEvents[0].Changeset.InitialState.ShouldEqual(_initialState);
}
