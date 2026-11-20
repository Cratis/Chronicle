// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_DormantObserverGauge;

public class when_queues_hold_dormant_observers : given.a_dormant_observer_gauge
{
    Dictionary<string, int> _observed;

    void Establish()
    {
        _gauge.Register(_firstQueue, () => 3);
        _gauge.Register(_firstQueue, () => 2);
        _gauge.Register(_otherNamespace, () => 7);
    }

    void Because() => _observed = Observed();

    [Fact] void should_sum_the_queues_of_a_namespace() => _observed["namespace"].ShouldEqual(5);
    [Fact] void should_report_each_namespace_on_its_own() => _observed["other-namespace"].ShouldEqual(7);
}
