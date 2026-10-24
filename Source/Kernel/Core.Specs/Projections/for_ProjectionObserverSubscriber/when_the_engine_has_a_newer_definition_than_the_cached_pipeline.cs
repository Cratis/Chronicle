// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

/// <summary>
/// A subscriber that missed the definition notification (the Projection grain was deactivated or re-placed) and whose
/// old definition still accepts the event would keep applying the old pipeline, including during the replay the change
/// scheduled (#5071). The silo-local engine already holds the new definition, so its fingerprint reveals the change.
/// </summary>
public class when_the_engine_has_a_newer_definition_than_the_cached_pipeline : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _event;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventType(new("accepted-event", 1));
        _event = _event with { Context = _event.Context with { SequenceNumber = 42 } };
        _projection.DefinitionFingerprint.Returns("old");

        var refreshed = Substitute.For<Engine.IProjection>();
        refreshed.DefinitionFingerprint.Returns("new");
        refreshed.Accepts(Arg.Any<Concepts.Events.EventType>()).Returns(true);
        _engineProjections.TryGet(_key.EventStore, _key.Namespace, _key.ObserverId, out Arg.Any<Engine.IProjection?>())
            .Returns(_ =>
            {
                _[3] = refreshed;
                return true;
            });
        _engineProjections.GetCurrent(_key.EventStore, _key.Namespace, _key.ObserverId).Returns(refreshed);

        var updated = _definition with { IsRewindable = false };
        _storage.GetEventStore(_key.EventStore).Projections.Get(_key.ObserverId).Returns(updated);
        _refreshedPipeline.Handle(_event).Returns(ProjectionEventContext.Empty(Substitute.For<Changes.IObjectComparer>(), _event));
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_handle_the_event_with_the_refreshed_pipeline() => _ = _refreshedPipeline.Received(1).Handle(_event);
    [Fact] void should_not_handle_the_event_with_the_cached_pipeline() => _ = _cachedPipeline.DidNotReceive().Handle(_event);
    [Fact] void should_evict_the_cached_pipeline_once() => _pipelines.Received(1).EvictFor(_key.EventStore, _key.Namespace, _key.ObserverId);
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
