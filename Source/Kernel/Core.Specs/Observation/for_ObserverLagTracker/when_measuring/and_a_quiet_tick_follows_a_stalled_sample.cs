// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_a_quiet_tick_follows_a_stalled_sample : given.a_tracker
{
    ObserverLagSample _result;

    void Establish()
    {
        Measure(0, 10);
        Measure(2, 11);
        Measure(4, 12);
    }

    void Because() => _result = Measure(5, 12);

    [Fact] void should_still_be_stalled() => _result.Status.ShouldEqual(ObserverLagStatus.Stalled);
}
