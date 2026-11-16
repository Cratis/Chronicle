// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.when_pruning;

public class and_an_observer_definition_exists : given.a_pruner
{
    void Establish()
    {
        _rows.Add(Replay("observer", true));
        _store.Observers.Has((ObserverId)"observer").Returns(true);
    }

    Task Because() => Prune();

    [Fact] void should_keep_the_row() => _removed.ShouldBeEmpty();
    [Fact] void should_report_no_removals() => _count.ShouldEqual(0);
}
