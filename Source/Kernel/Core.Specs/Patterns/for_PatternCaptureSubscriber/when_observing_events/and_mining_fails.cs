// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_observing_events;

/// <summary>
/// Mining is derived, secondary information. A failure must not stop the event sequence being observed for
/// everything else, so it is reported as this observer's failure and nothing more - and since the miner counts
/// nothing when it fails, the redelivered batch counts nothing twice.
/// </summary>
public class and_mining_fails : given.a_pattern_capture_subscriber
{
    static readonly PatternGroupingKey _scope = "some.user";

    ObserverSubscriberResult _result;

    void Establish() => _miner.Mine(Arg.Any<IEnumerable<EventFeatures>>()).Returns(_ => throw new Exception("mining broke"));

    async Task Because() =>
        _result = await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, _scope)],
            new ObserverSubscriberContext(null));

    [Fact] void should_answer_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_carry_the_failure() => _result.ExceptionMessages.ShouldContain("mining broke");
}
