// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_a_pinned_generation_batch_fails : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _first;
    AppendedEvent _second;
    AppendedEvent _earlier;
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        await _subscriber.OnProjectionDefinitionsChanged(_definition with { GenerationDelivery = EventGenerationDelivery.Pinned });
        _earlier = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new("EarlierEvent", 1), 41);
        _first = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new("PersonRegistered", 1), 42);
        _second = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new("PersonRegistered", 2), 42);
        _refreshedPipeline.Handle(_earlier).Returns(ProjectionEventContext.Empty(new ObjectComparer(), _earlier));
        _refreshedPipeline.HandleGenerations(Arg.Any<IEnumerable<AppendedEvent>>()).Returns(Task.FromException<IEnumerable<ProjectionEventContext>>(new MissingKeyResolverForEventType(_second.Context.EventType)));
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_earlier, _first, _second], new(null));

    [Fact] void should_fail_the_partition() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_acknowledge_a_partially_completed_physical_event() => _result.LastSuccessfulObservation.Value.ShouldEqual(41UL);
}
