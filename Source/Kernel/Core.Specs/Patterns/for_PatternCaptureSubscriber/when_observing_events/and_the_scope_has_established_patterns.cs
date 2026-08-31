// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriber.when_observing_events;

public class and_the_scope_has_established_patterns : given.a_pattern_capture_subscriber
{
    static readonly PatternGroupingKey _scope = "some.user";

    BehaviorPattern _established;

    void Establish()
    {
        _established = new BehaviorPattern(
            _scope,
            new FacetSet([new Facet(FacetName.CommandType, "some-command")]),
            15,
            1d,
            0.75d,
            15d,
            Occurred,
            Occurred);
        _patterns.GetForScope(_scope).Returns([_established]);
    }

    async Task Because()
    {
        await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(42UL, _scope)],
            new ObserverSubscriberContext(null));
        await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [EventAt(43UL, _scope)],
            new ObserverSubscriberContext(null));
    }

    [Fact] void should_restore_the_established_patterns_before_mining() =>
        Received.InOrder(() =>
        {
            _miner.Restore(_eventStore, _namespace, _scope, Arg.Is<IEnumerable<BehaviorPattern>>(patterns => patterns.Single() == _established));
            _miner.Observe(_eventStore, _namespace, Arg.Any<EventFeatures>());
            _miner.Observe(_eventStore, _namespace, Arg.Any<EventFeatures>());
        });

    [Fact] async Task should_only_read_the_established_patterns_once_per_activation() => await _patterns.Received(1).GetForScope(_scope);
    [Fact] void should_mine_every_event() => _miner.Received(2).Observe(_eventStore, _namespace, Arg.Any<EventFeatures>());
}
