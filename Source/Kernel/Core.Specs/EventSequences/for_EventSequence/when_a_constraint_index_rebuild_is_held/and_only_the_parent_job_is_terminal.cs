// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_only_the_parent_job_is_terminal : given.a_held_constraint_index_rebuild
{
    async Task Establish()
    {
        _stepStatus = JobStepStatus.Running;
        await HoldRebuild();
        _namespaceStorage.Jobs.GetJob(_jobId).Returns(Monads.Catch<JobState, Orleans.Storage.Jobs.JobError>.Success(new() { Status = JobStatus.CompletedWithFailures }));
    }

    async Task Because()
    {
        await PassLivenessCheck();
        _result = await AppendValue("current");
    }

    [Fact] async Task should_not_fold_while_the_step_can_write() => (await Values("source")).ShouldBeEmpty();
    [Fact] void should_keep_the_rebuild_refusal() => _result.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] async Task should_not_consult_parent_status() => await _namespaceStorage.Jobs.DidNotReceive().GetJob(_jobId);
}
