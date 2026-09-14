// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_observing_events;

public class and_no_event_names_a_scope : given.a_pattern_capture_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because() =>
        _result = await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, new PatternGroupingKey(string.Empty))],
            new ObserverSubscriberContext(null));

    [Fact] void should_answer_ok() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_answer_with_the_tail_of_the_batch() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_not_hand_anything_to_the_miner() => _miner.DidNotReceiveWithAnyArgs().Mine(default!);
}
