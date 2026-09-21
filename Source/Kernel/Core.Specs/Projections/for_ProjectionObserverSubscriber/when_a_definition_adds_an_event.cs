// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_a_definition_adds_an_event : given.a_subscriber_with_a_cached_pipeline
{
    ProjectionDefinition _updated;
    AppendedEvent _event;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _returnedPipeline.ShouldEqual(_cachedPipeline);
        var eventType = new EventType("the-new-event", 1);
        _event = AppendedEvent.EmptyWithEventType(eventType);
        _event = _event with { Context = _event.Context with { SequenceNumber = 42 } };
        _cachedPipeline.Handle(_event).Returns(Task.FromException<ProjectionEventContext>(new MissingKeyResolverForEventType(eventType)));
        _refreshedPipeline.Handle(_event).Returns(ProjectionEventContext.Empty(Substitute.For<IObjectComparer>(), _event));
        _updated = _definition with
        {
            From = new Dictionary<EventType, FromDefinition>
            {
                [eventType] = new(
                    new Dictionary<PropertyPath, string> { ["status"] = "$value(1)" },
                    "$eventSourceId",
                    string.Empty)
            }
        };
    }

    async Task Because()
    {
        await _subscriber.OnProjectionDefinitionsChanged(_updated);
        _result = await _subscriber.OnNext("the-partition", [_event], new(null));
    }

    [Fact] void should_evict_the_local_cached_pipeline() => _pipelines.Received(1).EvictFor(_key.EventStore, _key.Namespace, _key.ObserverId);
    [Fact] void should_acquire_a_refreshed_pipeline() => _returnedPipeline.ShouldEqual(_refreshedPipeline);
    [Fact] void should_deliver_the_new_event_to_the_refreshed_pipeline() => _ = _refreshedPipeline.Received(1).Handle(_event);
    [Fact] void should_not_deliver_the_new_event_to_the_stale_pipeline() => _ = _cachedPipeline.DidNotReceive().Handle(_event);
    [Fact] void should_handle_the_delivery_successfully() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_report_the_successfully_handled_event() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
    [Fact]
    void should_evict_before_acquiring_the_pipeline() => Received.InOrder(() =>
    {
        _pipelines.EvictFor(_key.EventStore, _key.Namespace, _key.ObserverId);
        _ = _pipelines.GetFor(_key.EventStore, _key.Namespace, _projection);
    });
}
