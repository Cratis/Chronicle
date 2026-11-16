// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.when_pruning;

public class and_a_reactor_definition_exists : given.a_pruner
{
    void Establish()
    {
        _rows.Add(Replay("reactor"));
        _store.Reactors.Has((ReactorId)"reactor").Returns(true);
    }

    Task Because() => Prune();

    [Fact] void should_keep_the_row() => _removed.ShouldBeEmpty();
}
