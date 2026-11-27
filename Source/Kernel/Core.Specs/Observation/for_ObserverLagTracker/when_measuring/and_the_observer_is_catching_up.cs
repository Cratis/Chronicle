// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_the_observer_is_catching_up : given.a_tracker
{
    ObserverLagSample _result;

    void Establish() => Measure(0, 10, catchingUp: true);

    void Because()
    {
        Measure(2, 11, catchingUp: true);
        _result = Measure(4, 12, catchingUp: true);
    }

    [Fact] void should_never_be_stalled() => _result.Status.ShouldEqual(ObserverLagStatus.Lagging);
}
