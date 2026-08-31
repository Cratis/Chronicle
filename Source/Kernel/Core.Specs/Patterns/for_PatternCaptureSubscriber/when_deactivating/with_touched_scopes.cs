// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_deactivating;

public class with_touched_scopes : given.a_pattern_capture_subscriber
{
    static readonly PatternGroupingKey _scope = "some.user";

    async Task Establish()
    {
        _miner.GetSurvivingPatterns(_eventStore, _namespace, _scope).Returns([]);
        await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, _scope)],
            new ObserverSubscriberContext(null));
    }

    async Task Because() => await _silo.DeactivateAsync(_subscriber);

    [Fact] void should_persist_what_the_interval_had_not_flushed_yet() => _patterns.Received(1).Save(Arg.Any<IEnumerable<BehaviorPattern>>());
}
