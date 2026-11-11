// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_deferring_recovery_for_admission : Specification
{
    TimeSpan _shortest;
    TimeSpan _middle;
    TimeSpan _longest;

    void Because()
    {
        _shortest = Observer.NextRecoveryAdmissionRetryDelay(0);
        _middle = Observer.NextRecoveryAdmissionRetryDelay(0.5);
        _longest = Observer.NextRecoveryAdmissionRetryDelay(1);
    }

    [Fact] void should_wait_at_least_the_shortest_delay() => _shortest.ShouldEqual(Observer.MinRecoveryAdmissionRetryDelay);
    [Fact] void should_spread_the_waits_between_the_bounds() => _middle.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_wait_at_most_the_longest_delay() => _longest.ShouldEqual(Observer.MaxRecoveryAdmissionRetryDelay);
}
