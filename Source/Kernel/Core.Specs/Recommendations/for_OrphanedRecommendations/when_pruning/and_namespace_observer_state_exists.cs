// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.when_pruning;

public class and_namespace_observer_state_exists : given.a_pruner
{
    void Establish()
    {
        _rows.Add(Replay("observer"));
        _observers.Get((ObserverId)"observer").Returns(new ObserverState { Identifier = "observer" });
    }

    Task Because() => Prune();

    [Fact] void should_keep_the_row() => _removed.ShouldBeEmpty();
}
