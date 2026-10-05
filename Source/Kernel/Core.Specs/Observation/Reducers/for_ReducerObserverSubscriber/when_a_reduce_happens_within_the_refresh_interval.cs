// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber;

/// <summary>
/// The common case - repeated batches delivered well inside the refresh window - must not pay for a rebuilt
/// pipeline on every delivery; only the first activation's pipeline is ever built.
/// </summary>
public class when_a_reduce_happens_within_the_refresh_interval : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _event;

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventType(new EventType("the-event", 1));
        _clock.Advance(ReducerObserverSubscriber.PipelineRefreshInterval - TimeSpan.FromSeconds(1));
    }

    async Task Because() => await _subscriber.OnNext("the-partition", [_event], new ObserverSubscriberContext(new Cratis.Chronicle.Concepts.Clients.ConnectedClient()));

    [Fact] void should_not_rebuild_the_pipeline() => _pipelineFactory.DidNotReceive().Create(_key.EventStore, _key.Namespace, Arg.Any<Cratis.Chronicle.Concepts.Observation.Reducers.ReducerDefinition>());
    [Fact] void should_deliver_through_the_pipeline_built_at_activation() => _ = _firstPipeline.Received(1).Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>());
    [Fact] void should_not_deliver_through_a_different_pipeline() => _ = _secondPipeline.DidNotReceive().Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>());
}
