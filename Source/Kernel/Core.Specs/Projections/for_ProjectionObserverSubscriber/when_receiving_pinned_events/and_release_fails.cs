// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_pinned_events;

public class and_release_fails : given.a_pinned_subscriber
{
    ObserverSubscriberResult _result;

    void Establish() => _migrations.MigrateToAllGenerations(Arg.Any<EventTypeDefinition>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>())
        .Returns(Task.FromException<IDictionary<EventTypeGeneration, ExpandoObject>>(new PinnedEventTypeGenerationUnavailable(_pin)));

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_fail_the_partition() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_acknowledge_the_event() => _result.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_hand_the_event_to_the_pipeline() => _ = _cachedPipeline.DidNotReceive().Handle(Arg.Any<AppendedEvent>());
}
