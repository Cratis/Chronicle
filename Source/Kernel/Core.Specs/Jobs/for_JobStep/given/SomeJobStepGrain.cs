// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Jobs.for_JobStep.given;

/// <summary>
/// A concrete job step whose performed work is observable from a specification.
/// </summary>
/// <param name="state">The persistent state.</param>
/// <param name="throttle">The throttle.</param>
/// <param name="logger">The logger.</param>
public class SomeJobStepGrain(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)] IPersistentState<JobStepState> state,
    IJobStepThrottle throttle,
    ILogger<SomeJobStepGrain> logger) : JobStep<SomeRequest, object, JobStepState>(state, throttle, logger), ISomeJobStepGrain
{
    /// <summary>
    /// Gets the completion that is resolved when <see cref="PerformStep"/> has run.
    /// </summary>
    public TaskCompletionSource Performed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets or sets whether <see cref="PerformStep"/> holds its work open until <see cref="Gate"/> is released -
    /// how a specification arranges a step that is genuinely running right now.
    /// </summary>
    public bool HoldPerformOpen { get; set; }

    /// <summary>
    /// Gets the gate a held-open <see cref="PerformStep"/> waits on.
    /// </summary>
    public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the job grain this step reports to - a substitute, wired to accept every report.
    /// </summary>
    public IJob ReportedJob { get; } = CreateAcceptingJob();

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(SomeRequest request) => Task.FromResult(Result<PrepareJobStepError>.Success());

    /// <inheritdoc/>
    protected override IJobStep<SomeRequest, object, JobStepState> GetReferenceToSelf() => this;

    /// <inheritdoc/>
    protected override IJob GetJob(GrainId jobGrainId) => ReportedJob;

    /// <inheritdoc/>
    protected override ValueTask InitializeState(SomeRequest request) => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(JobStepState currentState, CancellationToken cancellationToken)
    {
        Performed.TrySetResult();
        if (HoldPerformOpen)
        {
            await Gate.Task;
        }

        return JobStepResult.Succeeded();
    }

    /// <inheritdoc/>
    protected override ValueTask<object?> CreateCancelledResultFromCurrentState(JobStepState currentState) => ValueTask.FromResult<object?>(null);

    static IJob CreateAcceptingJob()
    {
        var job = Substitute.For<IJob>();
        job.OnStepSucceeded(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(Result<JobError>.Success());
        job.OnStepFailed(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(Result<JobError>.Success());
        job.OnStepStopped(Arg.Any<JobStepId>(), Arg.Any<JobStepResult>()).Returns(Result<JobError>.Success());
        return job;
    }
}
