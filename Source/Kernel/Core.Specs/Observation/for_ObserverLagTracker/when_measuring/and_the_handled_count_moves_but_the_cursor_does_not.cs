// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_the_handled_count_moves_but_the_cursor_does_not : given.a_tracker
{
    ObserverLagSample _result;

    void Establish()
    {
        Measure(0, 10);
        Measure(2, 11);
    }

    void Because() => _result = Measure(4, 12, count: 5);

    [Fact] void should_restart_the_window() => _result.Status.ShouldEqual(ObserverLagStatus.Lagging);
}
