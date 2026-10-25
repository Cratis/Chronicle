// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

/// <summary>
/// The fast path: when the engine's fingerprint equals the cached projection's, nothing is read from storage.
/// </summary>
public class when_the_engine_fingerprint_matches_the_cached_projection : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _event;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventType(new("accepted-event", 1));
        _event = _event with { Context = _event.Context with { SequenceNumber = 42 } };
        _projection.DefinitionFingerprint.Returns("same");

        var current = Substitute.For<Engine.IProjection>();
        current.DefinitionFingerprint.Returns("same");
        _engineProjections.TryGet(_key.EventStore, _key.Namespace, _key.ObserverId, out Arg.Any<Engine.IProjection?>())
            .Returns(_ =>
            {
                _[3] = current;
                return true;
            });
        _cachedPipeline.Handle(_event).Returns(ProjectionEventContext.Empty(Substitute.For<Changes.IObjectComparer>(), _event));
        _storage.ClearReceivedCalls();
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_handle_the_event_with_the_cached_pipeline() => _ = _cachedPipeline.Received(1).Handle(_event);
    [Fact] async Task should_not_read_the_current_projection() => await _engineProjections.DidNotReceive().GetCurrent(_key.EventStore, _key.Namespace, _key.ObserverId);
    [Fact] void should_not_read_shared_storage() => _ = _storage.DidNotReceive().GetEventStore(Arg.Any<Concepts.EventStoreName>());
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
