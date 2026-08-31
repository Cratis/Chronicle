// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;
using Orleans.TestKit;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_persisting;

public class and_storage_fails : given.a_pattern_capture_subscriber
{
    static readonly PatternGroupingKey _scope = "some.user";

    async Task Establish()
    {
        _miner.GetSurvivingPatterns(_eventStore, _namespace, _scope).Returns([]);
        _patterns.Save(Arg.Any<IEnumerable<BehaviorPattern>>()).Returns(
            _ => throw new Exception("storage broke"),
            _ => Task.CompletedTask);

        await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, _scope)],
            new ObserverSubscriberContext(null));

        // The first tick fails against storage; the scope must stay marked so the next tick tries again.
        await _silo.FireAllTimersAsync();
    }

    async Task Because() => await _silo.FireAllTimersAsync();

    [Fact] void should_retry_the_scope_on_the_next_tick() => _patterns.Received(2).Save(Arg.Any<IEnumerable<BehaviorPattern>>());
    [Fact] void should_remove_what_no_longer_survives_once_saving_succeeds() => _patterns.Received(1).RemoveAllExcept(_scope, Arg.Any<IEnumerable<FacetSetKey>>());
}
