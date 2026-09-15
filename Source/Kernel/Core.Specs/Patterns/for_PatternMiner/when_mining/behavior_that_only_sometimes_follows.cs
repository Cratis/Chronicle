// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_mining;

/// <summary>
/// Confidence is the rule "in this context, this action follows". Approving on three Monday mornings out of four
/// is a real pattern held with three quarters confidence - not a certainty, and not noise.
/// </summary>
public class behavior_that_only_sometimes_follows : given.a_pattern_miner
{
    IEnumerable<BehaviorPattern> _result;

    async Task Because()
    {
        for (var count = 0; count < 15; count++)
        {
            await _miner.Mine([Features("user-42", "ApproveExpenseReport")]);
        }

        for (var count = 0; count < 5; count++)
        {
            await _miner.Mine([Features("user-42", "RejectExpenseReport")]);
        }

        _result = await _miner.GetSurvivingPatterns("user-42");
    }

    BehaviorPattern Approving => _result.Single(_ =>
        _.Facets.ValueOf(FacetName.CommandType).Value == "ApproveExpenseReport" &&
        _.Facets.ValueOf(FacetName.Day).Value == "Monday" &&
        _.Facets.ValueOf(FacetName.TimeBucket).Value == "Morning");

    [Fact] void should_hold_the_approving_pattern_with_three_quarters_confidence() => Math.Round(Approving.Confidence.Value, 6).ShouldEqual(0.75d);
    [Fact] void should_hold_it_with_three_quarters_support() => Math.Round(Approving.Support.Value, 6).ShouldEqual(0.75d);
    [Fact] void should_not_hold_the_rejecting_pattern() =>
        _result.Any(_ => _.Facets.ValueOf(FacetName.CommandType).Value == "RejectExpenseReport").ShouldBeFalse();
}
