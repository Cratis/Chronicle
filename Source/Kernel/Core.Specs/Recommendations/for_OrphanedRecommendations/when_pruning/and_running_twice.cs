// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.when_pruning;

public class and_running_twice : given.a_pruner
{
    void Establish() => _rows.Add(Replay("observer", true));

    async Task Because()
    {
        await Prune();
        _rows.RemoveAll(_ => _removed.Contains(_.Id));
        await Prune();
    }

    [Fact] void should_remove_the_row_only_once() => _removed.Count.ShouldEqual(1);
    [Fact] void should_have_nothing_left_to_prune() => _count.ShouldEqual(0);
}
