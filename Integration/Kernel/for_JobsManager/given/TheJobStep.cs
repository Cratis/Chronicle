// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
namespace Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;

public class TheJobStep(
    TheJobStepProcessor jobStepProcessor,
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<TheJobStepState> state,
    IJobStepThrottle throttle,
    ILogger<JobStep<TheJobStepRequest, TheJobStepResult, TheJobStepState>> logger)
    : JobStep<TheJobStepRequest, TheJobStepResult, TheJobStepState>(state, throttle, logger), ITheJobStep
{
    ITheJobStep _selfGrainReference;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _selfGrainReference = this.AsReference<ITheJobStep>();
        return base.OnActivateAsync(cancellationToken);
    }

    protected override Task<Result<PrepareJobStepError>> PrepareStep(TheJobStepRequest request)
    {
        jobStepProcessor.JobStepPrepared(JobId, JobStepId, this.GetGrainType());
        return Task.FromResult(Result<PrepareJobStepError>.Success());
    }

    protected override ValueTask InitializeState(TheJobStepRequest request)
    {
        State.ShouldFail = request.ShouldFail;
        State.WaitTime = request.WaitTime;
        State.WaitCount = request.WaitCount;

        return ValueTask.CompletedTask;
    }
    protected override ValueTask<TheJobStepResult?> CreateCancelledResultFromCurrentState(TheJobStepState currentState) => new(new TheJobStepResult());

    protected override async Task<Catch<JobStepResult>> PerformStep(TheJobStepState currentState, CancellationToken cancellationToken)
    {
        try
        {
            await _selfGrainReference.IncrementPerformCalled();
            jobStepProcessor.PerformJobStep(JobId, JobStepId, currentState);
            await jobStepProcessor.WaitForStart();
            if (currentState.WaitCount >= currentState.NumTimesPerformCalled)
            {
                await Task.Delay(currentState.WaitTime, cancellationToken);
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return await HandleStopped();
            }
            if (currentState.ShouldFail)
            {
                throw new Exception("Should fail");
            }

            jobStepProcessor.JobStepCompleted(JobId, JobStepId, currentState, JobStepStatus.CompletedSuccessfully);
            return JobStepResult.Succeeded(new TheJobStepResult());
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            return await HandleStopped();
        }
        catch (Exception ex)
        {
            jobStepProcessor.JobStepCompleted(JobId, JobStepId, currentState, JobStepStatus.CompletedWithFailure);
            return JobStepResult.Failed(PerformJobStepError.Failed(ex));
        }

        async Task<Catch<JobStepResult>> HandleStopped()
        {
            await _selfGrainReference.IncrementStopped();
            jobStepProcessor.JobStepCompleted(JobId, JobStepId, currentState, JobStepStatus.Stopped);
            return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
        }
    }

    public Task SetCompleted()
    {
        State.Completed = true;
        return Task.CompletedTask;
    }
    public Task SetFailed()
    {
        State.Failed = true;
        return Task.CompletedTask;
    }
    public Task IncrementStopped()
    {
        State.NumTimesStopped++;
        return Task.CompletedTask;
    }
    public Task IncrementPerformCalled()
    {
        State.NumTimesPerformCalled++;
        return Task.CompletedTask;
    }
}
