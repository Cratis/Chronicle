// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_pinned_events;

public class and_the_generation_differs_from_the_pin : given.a_pinned_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_process_the_event_once() => _ = _cachedPipeline.Received(1).Handle(Arg.Any<AppendedEvent>());
    [Fact] void should_deliver_the_pinned_generation() => _delivered.Context.EventType.ShouldEqual(_pin);
    [Fact] void should_deliver_the_migrated_content() => ((IDictionary<string, object?>)_delivered.Content)["fullName"].ShouldEqual("Ada Lovelace");
    [Fact] void should_not_claim_a_durable_hash_for_the_migration() => _delivered.Context.Hash.ShouldEqual(EventHash.NotSet);
    [Fact] void should_keep_the_appended_generation() => _delivered.Context.AppendedGeneration.ShouldEqual(_event.Context.AppendedGeneration);
    [Fact] void should_acknowledge_after_processing() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_report_the_processed_sequence_number() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
}
