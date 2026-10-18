// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_an_unaccepted_event_arrives;

public class and_the_definition_changed_without_notification : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _event;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventType(new("new-event", 1));
        _event = _event with { Context = _event.Context with { SequenceNumber = 42 } };
        _projection.Accepts(_event.Context.EventType).Returns(false);
        var updated = _definition with { IsRewindable = false };
        _storage.GetEventStore(_key.EventStore).Projections.Get(_key.ObserverId).Returns(updated);
        _factory.Create(_key.EventStore, _key.Namespace, updated, Arg.Any<Concepts.ReadModels.ReadModelDefinition>(), Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>())
            .Returns(_ =>
            {
                _projection.Accepts(_event.Context.EventType).Returns(true);
                return _projection;
            });
        _refreshedPipeline.Handle(_event).Returns(ProjectionEventContext.Empty(Substitute.For<Changes.IObjectComparer>(), _event));
        _projectionGrain.ClearReceivedCalls();
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_handle_the_previously_unaccepted_event() => _ = _refreshedPipeline.Received(1).Handle(_event);
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_read_shared_storage_once() => _ = _storage.GetEventStore(_key.EventStore).Projections.Received(1).Get(_key.ObserverId);
    [Fact] void should_not_call_back_into_the_notifying_grain() => _ = _projectionGrain.DidNotReceive().GetDefinition();
}
