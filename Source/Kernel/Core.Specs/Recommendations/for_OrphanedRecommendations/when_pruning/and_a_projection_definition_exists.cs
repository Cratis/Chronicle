// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.when_pruning;

public class and_a_projection_definition_exists : given.a_pruner
{
    void Establish()
    {
        _rows.AddRange([Replay("projection"), Registration("projection", true)]);
        _store.Projections.Has((ProjectionId)"projection").Returns(true);
    }

    Task Because() => Prune();

    [Fact] void should_keep_the_rows() => _removed.ShouldBeEmpty();
}
