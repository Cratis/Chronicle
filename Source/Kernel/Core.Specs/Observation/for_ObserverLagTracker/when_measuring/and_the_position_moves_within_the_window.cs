// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_the_position_moves_within_the_window : given.a_tracker
{
    ObserverLagSample _result;

    void Establish()
    {
        Measure(0, 10);
        Measure(2, 11);
    }

    void Because() => _result = Measure(4, 12, next: 6, handled: 5, count: 5);

    [Fact] void should_not_be_stalled() => _result.Status.ShouldEqual(ObserverLagStatus.Lagging);
}
