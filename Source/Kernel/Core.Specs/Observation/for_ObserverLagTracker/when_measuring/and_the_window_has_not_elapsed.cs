// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_the_window_has_not_elapsed : given.a_tracker
{
    ObserverLagSample _result;

    void Establish() => Measure(0, 10);

    void Because() => _result = Measure(1, 12);

    [Fact] void should_not_be_stalled() => _result.Status.ShouldEqual(ObserverLagStatus.Lagging);
}
