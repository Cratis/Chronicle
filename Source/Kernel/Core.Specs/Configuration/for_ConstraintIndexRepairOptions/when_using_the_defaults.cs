// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConstraintIndexRepairOptions;

public class when_using_the_defaults : Specification
{
    readonly ConstraintIndexRepairOptions _options = new();

    [Fact] void should_attempt_three_times() => _options.UpdateAttempts.ShouldEqual(3);
    [Fact] void should_time_out_an_attempt_after_one_second() => _options.AttemptTimeout.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_budget_two_seconds_after_commit() => _options.PostCommitBudget.ShouldEqual(TimeSpan.FromSeconds(2));
    [Fact] void should_budget_two_seconds_for_a_timer_turn() => _options.TimerTurnBudget.ShouldEqual(TimeSpan.FromSeconds(2));
    [Fact] void should_budget_five_seconds_for_an_append_repair() => _options.AppendRepairBudget.ShouldEqual(TimeSpan.FromSeconds(5));
    [Fact] void should_repair_on_append_every_five_seconds() => _options.AppendRepairInterval.ShouldEqual(TimeSpan.FromSeconds(5));
    [Fact] void should_settle_poison_after_five_attempts() => _options.PoisonThreshold.ShouldEqual(5);
    [Fact] void should_cap_a_fold_at_one_hundred_thousand_keys() => _options.FoldKeyLimit.ShouldEqual(100000);
    [Fact] void should_cap_catch_up_exclusions_at_one_hundred_thousand() => _options.CatchUpExclusionLimit.ShouldEqual(100000);
    [Fact] void should_allow_four_catch_up_slots() => _options.CatchUpSlots.ShouldEqual(4);
    [Fact] void should_be_valid() => _options.Validate().ShouldBeEmpty();
}
