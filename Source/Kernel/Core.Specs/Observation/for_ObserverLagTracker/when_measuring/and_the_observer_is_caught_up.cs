// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_the_observer_is_caught_up : given.a_tracker
{
    ObserverLagSample _result;

    void Because() => _result = Measure(0, 4, next: 5);

    [Fact] void should_be_caught_up() => _result.Status.ShouldEqual(ObserverLagStatus.CaughtUp);
    [Fact] void should_have_a_distance_of_exactly_zero() => _result.Distance.ShouldEqual(0UL);
}
