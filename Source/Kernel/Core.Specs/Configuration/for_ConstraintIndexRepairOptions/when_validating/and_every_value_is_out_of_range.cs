// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConstraintIndexRepairOptions.when_validating;

public class and_every_value_is_out_of_range : Specification
{
    ConstraintIndexRepairOptions _options;
    string[] _members;

    void Establish() => _options = new()
    {
        UpdateAttempts = 0,
        AttemptTimeout = TimeSpan.Zero,
        PostCommitBudget = TimeSpan.Zero,
        TimerTurnBudget = TimeSpan.FromMilliseconds(250),
        AppendRepairBudget = TimeSpan.FromMilliseconds(250),
        AppendRepairInterval = TimeSpan.FromSeconds(-1),
        PoisonThreshold = 1,
        FoldKeyLimit = 0,
        CatchUpExclusionLimit = 0,
        CatchUpSlots = 0
    };

    void Because() => _members = _options.Validate().SelectMany(_ => _.MemberNames).ToArray();

    [Fact] void should_name_update_attempts() => _members.ShouldContain("ConstraintIndexRepair.UpdateAttempts");
    [Fact] void should_name_attempt_timeout() => _members.ShouldContain("ConstraintIndexRepair.AttemptTimeout");
    [Fact] void should_name_post_commit_budget() => _members.ShouldContain("ConstraintIndexRepair.PostCommitBudget");
    [Fact] void should_name_timer_turn_budget() => _members.ShouldContain("ConstraintIndexRepair.TimerTurnBudget");
    [Fact] void should_name_append_repair_budget() => _members.ShouldContain("ConstraintIndexRepair.AppendRepairBudget");
    [Fact] void should_name_append_repair_interval() => _members.ShouldContain("ConstraintIndexRepair.AppendRepairInterval");
    [Fact] void should_name_poison_threshold() => _members.ShouldContain("ConstraintIndexRepair.PoisonThreshold");
    [Fact] void should_name_fold_key_limit() => _members.ShouldContain("ConstraintIndexRepair.FoldKeyLimit");
    [Fact] void should_name_catch_up_exclusion_limit() => _members.ShouldContain("ConstraintIndexRepair.CatchUpExclusionLimit");
    [Fact] void should_name_catch_up_slots() => _members.ShouldContain("ConstraintIndexRepair.CatchUpSlots");
    [Fact] void should_report_nothing_else() => _members.Length.ShouldEqual(10);
}
