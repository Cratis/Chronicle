// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_mining;

/// <summary>
/// Somebody who always approves expense reports on Monday mornings should come out as exactly that, with full
/// confidence: in that context, the action always follows.
/// </summary>
public class a_recurring_behavior : given.a_pattern_miner
{
    IEnumerable<BehaviorPattern> _result;

    void Because()
    {
        for (var count = 0; count < 20; count++)
        {
            _miner.Observe(_eventStore, _namespace, Features("user-42", "ApproveExpenseReport"));
        }

        _result = _miner.GetSurvivingPatterns(_eventStore, _namespace, "user-42");
    }

    [Fact] void should_mine_the_full_combination() =>
        _result.Any(_ =>
            _.Facets.ValueOf(FacetName.CommandType).Value == "ApproveExpenseReport" &&
            _.Facets.ValueOf(FacetName.Day).Value == "Monday" &&
            _.Facets.ValueOf(FacetName.TimeBucket).Value == "Morning").ShouldBeTrue();

    [Fact] void should_be_fully_confident() => _result.All(_ => _.Confidence.Value == 1d).ShouldBeTrue();
    [Fact] void should_have_full_support() => _result.All(_ => _.Support.Value == 1d).ShouldBeTrue();
    [Fact] void should_count_every_occurrence() => _result.All(_ => _.Occurrences.Value == 20L).ShouldBeTrue();
    [Fact] void should_scope_every_pattern_to_the_user() => _result.All(_ => _.GroupingKey.Value == "user-42").ShouldBeTrue();
    [Fact] void should_not_mine_more_than_the_capped_combinations() => _result.All(_ => _.Specificity <= 3).ShouldBeTrue();
}
