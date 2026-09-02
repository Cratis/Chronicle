// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

/// <summary>
/// A caller that predates the grouping - the Time Machine, and every client built before there was a
/// choice - leaves the field unset, and must keep getting correlations.
/// </summary>
public class and_no_grouping_is_asked_for : given.a_projection_with_a_history
{
    IEnumerable<ReadModelSnapshot> _result;

    async Task Because() => _result = await AllSnapshots(string.Empty);

    [Fact] void should_group_by_correlation() => _result.Count().ShouldEqual(2);
}
