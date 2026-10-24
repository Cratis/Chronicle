// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_no_duplicate_claims_are_found : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    ReindexConstraintsStepState _reported;
    JobStepProgressMessage _previousMessage;

    async Task Establish() => _previousMessage = (await _step.GetState()).Progress.Message;

    async Task Because()
    {
        _result = await _step.BeginTestRun().Perform(_state);
        _reported = await _step.GetState();
    }

    [Fact] void should_complete_the_scan()
    {
        _result.TryGetResult(out var result).ShouldBeTrue();
        result.TryGetError(out _).ShouldBeFalse();
    }
    [Fact] void should_not_report_any_collisions() => _reported.CollisionCounts.ShouldBeEmpty();
    [Fact] void should_leave_the_message_unchanged() => _reported.Progress.Message.ShouldEqual(_previousMessage);
}
