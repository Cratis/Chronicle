// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_observing_events;

/// <summary>
/// Capture switched off after the observer subscribed: the events are acknowledged and dropped before any feature is
/// extracted, so the miner (and its grain and storage) is never involved.
/// </summary>
public class and_capture_is_off : given.a_pattern_capture_subscriber
{
    ObserverSubscriberResult _result;

    void Establish() => _options.PatternDetection.Overrides[_eventStore.Value] = false;

    async Task Because() =>
        _result = await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, "user-42")],
            new ObserverSubscriberContext(null));

    [Fact] void should_answer_ok() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_answer_with_the_tail_of_the_batch() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_not_extract_anything() => _extractor.DidNotReceiveWithAnyArgs().Extract(default!);
    [Fact] void should_not_hand_anything_to_the_miner() => _miner.DidNotReceiveWithAnyArgs().Mine(default!);
}
