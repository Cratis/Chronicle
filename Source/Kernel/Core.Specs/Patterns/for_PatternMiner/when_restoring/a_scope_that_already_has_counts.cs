// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_restoring;

/// <summary>
/// Restoring is only meaningful into the absence a restart leaves behind. A scope that already holds live counts
/// has counted things the persisted patterns have not, so a restore into it must be ignored rather than merged.
/// </summary>
public class a_scope_that_already_has_counts : given.a_pattern_miner
{
    IEnumerable<BehaviorPattern> _surviving;

    void Establish()
    {
        for (var count = 0; count < 20; count++)
        {
            _miner.Observe(_eventStore, _namespace, Features("user-42", "ApproveExpenseReport"));
        }
    }

    void Because()
    {
        var stale = new BehaviorPattern(
            "user-42",
            new FacetSet([new Facet(FacetName.CommandType, "SomethingElse")]),
            99,
            1d,
            0.9d,
            99d,
            Occurred,
            Occurred);

        _miner.Restore(_eventStore, _namespace, "user-42", [stale]);
        _surviving = _miner.GetSurvivingPatterns(_eventStore, _namespace, "user-42");
    }

    [Fact] void should_keep_the_live_counts() =>
        _surviving.Where(_ => _.Facets.ValueOf(FacetName.CommandType).Value == "ApproveExpenseReport").All(_ => _.Occurrences.Value == 20L).ShouldBeTrue();

    [Fact] void should_ignore_what_the_restore_carried() =>
        _surviving.Any(_ => _.Facets.ValueOf(FacetName.CommandType).Value == "SomethingElse").ShouldBeFalse();
}
