// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_Projection;

public class when_next_event_is_not_of_interest : given.a_projection
{
    bool _observed;
    AppendedEvent _event;
    Changeset<AppendedEvent, ExpandoObject> _changeset;
    IObjectComparer _objectsComparer;

    void Establish()
    {
        var eventType = new EventType("cb1f33dd-8725-4bd2-a1a1-f372d352a7c6", 1);
        projection.SetEventTypesWithKeyResolvers(
            [
                    new(eventType, keyResolvers.FromEventSourceId)
            ],
            [eventType],
            new Dictionary<EventType, ProjectionOperationType>());

        _event = new(
            new(
                new("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
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
                [],
                EventHash.NotSet),
            new ExpandoObject());

        _objectsComparer = Substitute.For<IObjectComparer>();
        _objectsComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<AppendedEvent>(), out Arg.Any<IEnumerable<PropertyDifference>>()).Returns(true);

        _changeset = new(_objectsComparer, _event, new());
        projection.Event.Subscribe(_ => _observed = true);
    }

    void Because() => projection.OnNext(new(new(_event.Context.EventSourceId, ArrayIndexers.NoIndexers), _event, _changeset, ProjectionOperationType.From, false));

    [Fact] void should_not_be_observed() => _observed.ShouldBeFalse();
}
