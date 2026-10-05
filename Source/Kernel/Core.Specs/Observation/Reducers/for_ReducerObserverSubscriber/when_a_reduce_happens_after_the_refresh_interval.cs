// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber;

/// <summary>
/// A read model's definition - its container name, in particular - can change on a connected client's
/// reconnect without this already-activated partition grain ever being told. Once the refresh interval has
/// elapsed, the next delivery must rebuild the pipeline from the read model's current definition rather than
/// keep delivering through the one built at activation (#4569).
/// </summary>
public class when_a_reduce_happens_after_the_refresh_interval : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _event;

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventType(new EventType("the-event", 1));
        _clock.Advance(ReducerObserverSubscriber.PipelineRefreshInterval);
    }

    async Task Because() => await _subscriber.OnNext("the-partition", [_event], new ObserverSubscriberContext(new ConnectedClient()));

    [Fact] void should_rebuild_the_pipeline() => _pipelineFactory.Received(1).Create(_key.EventStore, _key.Namespace, Arg.Any<Cratis.Chronicle.Concepts.Observation.Reducers.ReducerDefinition>());
    [Fact] void should_deliver_through_the_refreshed_pipeline() => _ = _secondPipeline.Received(1).Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>());
    [Fact] void should_not_deliver_through_the_stale_pipeline() => _ = _firstPipeline.DidNotReceive().Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>());
}
