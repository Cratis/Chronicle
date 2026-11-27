// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Jobs.Stages;
using Cratis.Choreography.Serialization;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraints;

public class when_a_step_fails : given.a_reindex_job
{
    JobStepId _stepId;
    JobSummary? _summary;

    async Task Establish()
    {
        _silo.AddService(CratisJsonSerializer.CreateSerializerOptions());
        var step = Substitute.For<IReindexConstraintsStep>();
        step.Prepare(Arg.Any<object>(), Arg.Any<JobStepStage>()).Returns(Task.FromResult(Result<PrepareJobStepError>.Success()));
        step.Start(Arg.Any<GrainId>()).Returns(Task.FromResult<Result<StartJobStepError>>(StartJobStepError.AlreadyStarted));
        _silo.AddProbe<IReindexConstraintsStep>(identity =>
        {
            _stepId = Guid.Parse(identity.ToString().Split('+')[0]);
            return step;
        });

        var started = await _job.Start(new(EventSequenceId.Log, [new("unique", true, [])]));
        if (started.TryGetError(out var error))
        {
            var details = _state.State.StatusChanges.LastOrDefault();
            throw new InvalidOperationException($"Reindex job did not start: {error}. {string.Join("; ", details?.ExceptionMessages ?? [])} {details?.ExceptionStackTrace}");
        }
    }

    async Task Because()
    {
        await _job.OnStepFailed(_stepId, JobStepResult.Failed("Reindex failed"));
        _summary = await ReadJobById();
    }

    [Fact] void should_not_clear_the_job_state() => _state.Stats.Clears.ShouldEqual(0);
    [Fact] void should_find_the_failed_job_by_id_after_finalization() => (_summary?.Status ?? JobStatus.None).ShouldEqual(JobStatus.CompletedWithFailures);
}
