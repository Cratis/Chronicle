// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_the_gap_between_readings_exceeds_validity : given.a_tracker
{
    ObserverLagSample _result;

    void Establish()
    {
        Measure(0, 10);
        Measure(2, 11);
    }

    void Because() => _result = Measure(30, 12);

    [Fact] void should_not_count_the_gap_as_a_stall() => _result.Status.ShouldEqual(ObserverLagStatus.Lagging);
}
