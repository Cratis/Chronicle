// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_observing_events;

public class and_the_persistence_interval_has_not_elapsed : given.a_pattern_capture_subscriber
{
    static readonly PatternGroupingKey _scope = "some.user";

    ObserverSubscriberResult _result;

    async Task Because() =>
        _result = await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, _scope), EventAt(43UL, _scope)],
            new ObserverSubscriberContext(null));

    [Fact] void should_answer_ok() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_answer_with_the_tail_of_the_batch() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_mine_every_event() => _miner.Received(2).Observe(_eventStore, _namespace, Arg.Is<EventFeatures>(features => features.GroupingKey == _scope));
    [Fact] void should_not_touch_storage() => _patterns.DidNotReceiveWithAnyArgs().Save(default!);
    [Fact] void should_not_remove_anything() => _patterns.DidNotReceiveWithAnyArgs().RemoveAllExcept(default!, default!);
}
