// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_observing_event_stores;

public class and_initialization_keeps_failing : given.a_live_event_store_list
{
    List<long> _attemptTimes;

    void Establish()
    {
        _attemptTimes = [];
        _initialization.EnsureAll(Arg.Any<IEnumerable<EventStoreName>>()).Returns(_ =>
        {
            _attemptTimes.Add(_scheduler.Clock / TimeSpan.TicksPerSecond);
            return [_stores[0]];
        });
    }

    void Because()
    {
        Observe();
        _storedSets.OnNext(_stores);
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(121).Ticks);
    }

    [Fact] void should_double_the_delay_up_to_thirty_seconds_and_keep_retrying() => _attemptTimes.ShouldEqual([0L, 1L, 3L, 7L, 15L, 31L, 61L, 91L, 121L]);
    [Fact] void should_not_emit_an_unchanged_initialized_set() => _emissions.Count.ShouldEqual(1);
}
