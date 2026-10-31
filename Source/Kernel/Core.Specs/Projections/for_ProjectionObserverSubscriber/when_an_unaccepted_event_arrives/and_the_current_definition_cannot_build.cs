// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_an_unaccepted_event_arrives;

public class and_the_current_definition_cannot_build : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _event;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventType(new("new-event", 1));
        _projection.Accepts(_event.Context.EventType).Returns(false);
        var updated = _definition with { IsRewindable = false };
        _storage.GetEventStore(_key.EventStore).Projections.Get(_key.ObserverId).Returns(updated);
        _factory.Create(_key.EventStore, _key.Namespace, updated, Arg.Any<Concepts.ReadModels.ReadModelDefinition>(), Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>())
            .Returns(Task.FromException<Engine.IProjection>(new MissingKeyResolverForEventType(_event.Context.EventType)));
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_fail_instead_of_skipping() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_advance_the_observer() => _result.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
}
