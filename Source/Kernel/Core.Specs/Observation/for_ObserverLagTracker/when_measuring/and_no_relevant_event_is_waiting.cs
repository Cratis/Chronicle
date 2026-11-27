// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_no_relevant_event_is_waiting : given.a_tracker
{
    ObserverLagSample _result;

    void Establish() => Measure(0, 10, relevance: ObserverLagRelevance.None);

    void Because()
    {
        Measure(2, 11, relevance: ObserverLagRelevance.None);
        _result = Measure(4, 12, relevance: ObserverLagRelevance.None);
    }

    [Fact] void should_never_be_stalled() => _result.Status.ShouldEqual(ObserverLagStatus.Lagging);
}
