// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_mining;

/// <summary>
/// A habit is a person's. Mining both users into one sketch would let the busier one's routine outvote the other's
/// and would make "what does this user usually do" unanswerable.
/// </summary>
public class behavior_for_two_users : given.a_pattern_miner
{
    IEnumerable<BehaviorPattern> _forFirstUser;
    IEnumerable<BehaviorPattern> _forSecondUser;

    void Because()
    {
        for (var count = 0; count < 20; count++)
        {
            _miner.Observe(Features("user-42", "ApproveExpenseReport"));
        }

        for (var count = 0; count < 20; count++)
        {
            _miner.Observe(Features("user-7", "SubmitExpenseReport", DayOfWeek.Friday, TimeBucket.Evening));
        }

        _forFirstUser = _miner.GetSurvivingPatterns("user-42");
        _forSecondUser = _miner.GetSurvivingPatterns("user-7");
    }

    [Fact] void should_mine_only_the_first_user_behavior_for_the_first_user() =>
        _forFirstUser.All(_ => _.Facets.ValueOf(FacetName.CommandType) != new FacetValue("SubmitExpenseReport")).ShouldBeTrue();

    [Fact] void should_mine_only_the_second_user_behavior_for_the_second_user() =>
        _forSecondUser.All(_ => _.Facets.ValueOf(FacetName.CommandType) != new FacetValue("ApproveExpenseReport")).ShouldBeTrue();

    [Fact] void should_hold_the_first_user_day() =>
        _forFirstUser.Any(_ => _.Facets.ValueOf(FacetName.Day).Value == "Monday").ShouldBeTrue();

    [Fact] void should_hold_the_second_user_day() =>
        _forSecondUser.Any(_ => _.Facets.ValueOf(FacetName.Day).Value == "Friday").ShouldBeTrue();

    [Fact] void should_be_fully_confident_for_each_user_on_their_own() =>
        _forFirstUser.Concat(_forSecondUser).All(_ => _.Confidence.Value == 1d).ShouldBeTrue();
}
