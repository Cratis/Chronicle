// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_state;

public class and_the_observer_was_released_longer_ago_than_the_zero_tail : given.the_observer_gauges
{
    IReadOnlyList<RecordedMeasurement> _perObserver;

    void Establish()
    {
        var entry = _gauges.Register(_observerKey, ObserverType.Reactor);
        entry.Publish(new(ObserverRunningState.Quarantined, 1, 1));
        Collect();
        entry.Release();
        _clock.Advance(ObserverGauges.ZeroTail);
    }

    void Because() => _perObserver = [.. Collect().Where(_ => _.TagMap.ContainsKey("cratis.observer.id"))];

    [Fact] void should_report_nothing_for_the_observer() => _perObserver.ShouldBeEmpty();
}
