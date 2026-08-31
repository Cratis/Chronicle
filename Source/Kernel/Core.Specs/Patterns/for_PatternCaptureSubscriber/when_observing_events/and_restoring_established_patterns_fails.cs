// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_observing_events;

/// <summary>
/// Mining a scope whose established patterns could not be read would rewrite it from a fresh sketch and wipe what
/// was established. Failing the batch instead redelivers it later - nothing was mined yet, so the retry counts
/// nothing twice.
/// </summary>
public class and_restoring_established_patterns_fails : given.a_pattern_capture_subscriber
{
    static readonly PatternGroupingKey _scope = "some.user";

    ObserverSubscriberResult _result;

    void Establish() => _patterns.GetForScope(_scope).Returns<IEnumerable<BehaviorPattern>>(_ => throw new Exception("storage broke"));

    async Task Because() =>
        _result = await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, _scope)],
            new ObserverSubscriberContext(null));

    [Fact] void should_answer_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_carry_the_failure() => _result.ExceptionMessages.ShouldContain("storage broke");
    [Fact] void should_not_mine_anything() => _miner.DidNotReceiveWithAnyArgs().Observe(default!, default!, default!);
}
