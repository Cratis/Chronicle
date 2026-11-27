// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_relevance_could_not_be_determined : given.a_tracker
{
    ObserverLagSample _result;

    void Establish() => Measure(0, 10, relevance: ObserverLagRelevance.Unknown);

    void Because()
    {
        Measure(2, 11, relevance: ObserverLagRelevance.Unknown);
        _result = Measure(4, 12, relevance: ObserverLagRelevance.Unknown);
    }

    [Fact] void should_never_be_stalled() => _result.Status.ShouldEqual(ObserverLagStatus.Lagging);
}
