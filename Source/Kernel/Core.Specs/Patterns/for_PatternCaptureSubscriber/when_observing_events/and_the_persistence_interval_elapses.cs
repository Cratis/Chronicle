// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;
using Orleans.TestKit;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_observing_events;

public class and_the_persistence_interval_elapses : given.a_pattern_capture_subscriber
{
    static readonly PatternGroupingKey _scope = "some.user";
    static readonly FacetSet _facets = new([new Facet(FacetName.CommandType, "some-command")]);

    BehaviorPattern _surviving;

    async Task Establish()
    {
        _surviving = new BehaviorPattern(_scope, _facets, 10, 0.8, 0.2, 10, Occurred, Occurred);
        _miner.GetSurvivingPatterns(_eventStore, _namespace, _scope).Returns([_surviving]);

        await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, _scope), EventAt(43UL, _scope)],
            new ObserverSubscriberContext(null));
    }

    async Task Because() => await _silo.FireAllTimersAsync();

    [Fact] void should_save_the_surviving_patterns_once_for_the_touched_scope() => _patterns.Received(1).Save(Arg.Is<IEnumerable<BehaviorPattern>>(patterns => patterns.Single() == _surviving));
    [Fact] void should_remove_everything_no_longer_surviving_for_the_touched_scope() => _patterns.Received(1).RemoveAllExcept(_scope, Arg.Is<IEnumerable<FacetSetKey>>(keys => keys.Single() == _facets.Key));
}
