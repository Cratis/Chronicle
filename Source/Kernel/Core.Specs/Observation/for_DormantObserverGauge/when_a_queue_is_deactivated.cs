// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_DormantObserverGauge;

public class when_a_queue_is_deactivated : given.a_dormant_observer_gauge
{
    IDisposable _registration;
    Dictionary<string, int> _before;
    Dictionary<string, int> _after;
    Dictionary<string, int> _afterReleasingTwice;

    void Establish() => _registration = _gauge.Register(_firstQueue, () => 4);

    void Because()
    {
        _before = Observed();
        _registration.Dispose();
        _after = Observed();
        _registration.Dispose();
        _afterReleasingTwice = Observed();
    }

    [Fact] void should_have_counted_the_queue_while_it_was_active() => _before["namespace"].ShouldEqual(4);
    [Fact] void should_report_zero_rather_than_dropping_the_series() => _after["namespace"].ShouldEqual(0);
    [Fact] void should_not_leak_or_go_negative_when_released_again() => _afterReleasingTwice["namespace"].ShouldEqual(0);
}
