// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_OrphanedRecommendations.when_pruning;

public class and_subjects_are_missing : given.a_pruner
{
    void Establish() => _rows.AddRange([Replay("observer"), Replay("ignored-observer", true), Registration("projection"), Registration("ignored-projection", true)]);

    Task Because() => Prune();

    [Fact] void should_remove_ignored_and_visible_rows() => _removed.ShouldContainOnly(_rows.Select(_ => _.Id));
    [Fact] void should_report_the_number_removed() => _count.ShouldEqual(4);
}
