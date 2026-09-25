// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

/// <summary>
/// The mirror of <see cref="when_a_definition_adds_an_event"/>, and the case that left a projection wedged
/// mid-replay (#3722). Dropping a child collection removes its event type from the projection, but the observer's
/// subscription is narrowed after the subscriber is told about the new definition - and the replay that same
/// change schedules reads through that subscription. So events the projection no longer takes part in keep
/// arriving for a while. Resolving a key for one throws, which failed the partition and stopped the replay.
/// The definition is the authority on what this projection observes, so an event outside it is skipped, and the
/// observer still moves past it rather than stalling on an event nothing will ever handle.
/// </summary>
public class when_a_definition_drops_an_event : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _event;
    ObserverSubscriberResult _result;

    void Establish()
    {
        var eventType = new EventType("the-dropped-event", 1);
        _event = AppendedEvent.EmptyWithEventType(eventType);
        _event = _event with { Context = _event.Context with { SequenceNumber = 42 } };
        _projection.Accepts(eventType).Returns(false);
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_not_hand_the_event_to_the_pipeline() => _ = _cachedPipeline.DidNotReceive().Handle(_event);
    [Fact] void should_not_fail_the_partition() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_move_the_observer_past_the_event() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
}
