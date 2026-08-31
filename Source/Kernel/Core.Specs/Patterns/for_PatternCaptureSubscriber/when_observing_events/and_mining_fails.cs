// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_observing_events;

public class and_mining_fails : given.a_pattern_capture_subscriber
{
    static readonly PatternGroupingKey _scope = "some.user";

    ObserverSubscriberResult _result;

    void Establish() => _miner.When(miner => miner.Observe(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventFeatures>())).Throw(new Exception("mining broke"));

    async Task Because() =>
        _result = await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, _scope)],
            new ObserverSubscriberContext(null));

    [Fact] void should_answer_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_carry_the_failure() => _result.ExceptionMessages.ShouldContain("mining broke");
}
