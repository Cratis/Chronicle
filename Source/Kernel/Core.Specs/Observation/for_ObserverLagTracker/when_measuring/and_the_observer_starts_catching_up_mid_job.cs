// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_the_observer_starts_catching_up_mid_job : given.a_tracker
{
    ObserverLagSample _result;
    ObserverLagSample _afterwards;

    void Establish()
    {
        Measure(0, 10);
        Measure(2, 11);
    }

    void Because()
    {
        _result = Measure(4, 12, catchingUp: true);
        _afterwards = Measure(5, 13);
    }

    [Fact] void should_not_be_stalled() => _result.Status.ShouldEqual(ObserverLagStatus.Lagging);
    [Fact] void should_have_forgotten_the_baseline() => _afterwards.Status.ShouldEqual(ObserverLagStatus.Lagging);
}
