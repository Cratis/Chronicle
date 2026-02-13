// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Cratis.Reflection;

namespace Cratis.Chronicle.Grains.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    /// <inheritdoc/>
    public async Task<Result<JobError>> OnStepSucceeded(JobStepId stepId, JobStepResult jobStepResult)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        _logger.StepSuccessfullyCompleted(stepId);
        State.Progress.SuccessfulSteps++;
        return await PerformStepEventHandling(stepId, jobStepResult);
    }

    /// <inheritdoc/>
    public async Task<Result<JobError>> OnStepStopped(JobStepId stepId, JobStepResult jobStepResult)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        _logger.StepStopped(stepId);
        State.Progress.StoppedSteps++;
        return await PerformStepEventHandling(stepId, jobStepResult);
    }

    /// <inheritdoc/>
    public async Task<Result<JobError>> OnStepFailed(JobStepId stepId, JobStepResult jobStepResult)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        _logger.StepFailed(stepId);
        State.Progress.FailedSteps++;
        return await PerformStepEventHandling(stepId, jobStepResult);
    }

    /// <summary>
    /// Called when a step has completed successfully, completed with failure or stopped.
    /// </summary>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the step.</param>
    /// <param name="result"><see cref="JobStepResult"/> for the step.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnStepCompletedOrStopped(JobStepId jobStepId, JobStepResult result) => Task.CompletedTask;

    /// <summary>
    /// Create a new <see cref="IJobStep"/>.
    /// </summary>
    /// <param name="request">The request associated with the step.</param>
    /// <typeparam name="TJobStep">The type of job step to create.</typeparam>
    /// <returns>A new instance of the job step.</returns>
    protected JobStepDetails CreateStep<TJobStep>(object request)
        where TJobStep : IJobStep
    {
        var jobStepId = JobStepId.New();
        var jobId = this.GetPrimaryKey(out var keyExtension);
        var jobKey = (JobKey)keyExtension!;
        var jobStepType = typeof(TJobStep)
            .AllBaseAndImplementingTypes()
            .First(
                _ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IJobStep<,,>));
        var resultType = jobStepType.GetGenericArguments()[1];
        return new(
            typeof(TJobStep),
            jobStepId,
            new(jobId, jobKey.EventStore, jobKey.Namespace),
            request,
            resultType);
    }

    /// <summary>
    /// Start the job.
    /// </summary>
    /// <param name="request">The request associated with the job.</param>
    /// <returns>Collection of <see cref="JobStepDetails"/> .</returns>
    protected abstract Task<IImmutableList<JobStepDetails>> PrepareSteps(TRequest request);

    async Task<Result<JobError>> PerformStepEventHandling(JobStepId stepId, JobStepResult jobStepResult)
    {
        try
        {
            _ = await WriteState();
            var handleCompletedStepResult = await HandleJobStepCompletedOrStopped(stepId, jobStepResult);
            return handleCompletedStepResult.Match(
                _ => Result.Success<JobError>(),
                error =>
                {
                    _logger.FailedHandlingCompletedJobStep(stepId, error);
                    return Result.Failed(error);
                });
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return JobError.UnknownError;
        }
    }

    async Task<Result<JobError>> HandleJobStepCompletedOrStopped(JobStepId stepId, JobStepResult result)
    {
        try
        {
            await OnStepCompletedOrStopped(stepId, result);
            var handleCompletionResult = await HandleCompletion();
            return await HandleCompletionResult(handleCompletionResult);
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return JobError.UnknownError;
        }
        finally
        {
            await UnsubscribeJobStep(_jobStepGrains![stepId].AsReference<IJobObserver>());
            _jobStepGrains.Remove(stepId, out _);
        }
    }

    async Task<Result<JobError>> HandleCompletionResult(Result<HandleCompletionSuccess, JobError> completionResult)
    {
        if (completionResult.TryGetError(out var handleCompletionError))
        {
            return handleCompletionError;
        }
        var needsToWriteState = completionResult.AsT0 switch
        {
            HandleCompletionSuccess.ClearedState => false,
            _ => true
        };
        if (!needsToWriteState)
        {
            return Result<JobError>.Success();
        }
        var writeStateResult = await WriteState();
        return writeStateResult.Match(
            _ => Result<JobError>.Success(),
            ex =>
            {
                _logger.FailedUpdatingStateAfterHandlingCompletion(ex);
                return JobError.PersistStateError;
            });
    }

    ReadOnlyDictionary<JobStepId, JobStepGrainAndRequest> CreateGrainsFromJobSteps(IImmutableList<JobStepDetails> jobSteps) =>
        jobSteps.ToDictionary(
            details => details.Id,
            details => new JobStepGrainAndRequest(GetJobStepGrain(details), details.Request)).AsReadOnly();

    async Task<Dictionary<JobStepId, IJobStep>> GetIdAndGrainReferenceJobSteps(params JobStepStatus[] statuses)
    {
        var getJobSteps = await Storage.JobSteps.GetForJob(JobId, statuses);
        getJobSteps.RethrowError();
        var jobSteps = getJobSteps.AsT0;
        return jobSteps.ToDictionary(jobStep => jobStep.Id.JobStepId, GetJobStepGrain);
    }
    Task<Dictionary<JobStepId, IJobStep>> GetIdAndGrainReferenceForNonCompletedJobSteps() =>
        GetIdAndGrainReferenceJobSteps(JobStepStatus.Running, JobStepStatus.Scheduled, JobStepStatus.Unknown, JobStepStatus.Stopped);

    IJobStep GetJobStepGrain(JobStepDetails details) => (GrainFactory.GetGrain(details.Type, details.Id, keyExtension: details.Key) as IJobStep)!;
    IJobStep GetJobStepGrain(JobStepState state) => (GrainFactory.GetGrain((Type)state.Type, state.Id.JobStepId, keyExtension: new JobStepKey(state.Id.JobId, JobKey.EventStore, JobKey.Namespace)) as IJobStep)!;

    async Task<Result<StartJobError>> PrepareAndStartRunningAllSteps(TRequest request)
    {
        try
        {
            var grainId = this.GetGrainId();
            var steps = await PrepareSteps(request);
            _ = await SetTotalSteps(steps.Count);
            if (steps.Count == 0)
            {
                _logger.NoJobStepsToStart();
                _ = HandleCompletionResult(await HandleCompletion());
                return StartJobError.NoJobStepsToStart;
            }

            _logger.PreparingJobSteps(steps.Count);
            return await PrepareAndStartAllJobSteps(grainId, steps);
        }
        catch (Exception ex)
        {
            _logger.ErrorPreparingJobSteps(ex);
            _ = await WriteStatusChanged(JobStatus.Failed, ex);
            return StartJobError.Unknown;
        }
    }

    async Task<Result<StartJobError>> PrepareAndStartAllJobSteps(GrainId grainId, IImmutableList<JobStepDetails> jobSteps)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        _ = await WriteStatusChanged(JobStatus.PreparingSteps);

        var jobStepGrainAndRequests = CreateGrainsFromJobSteps(jobSteps);
        _jobStepGrains = jobStepGrainAndRequests.ToDictionary(_ => _.Key, _ => _.Value.Grain);
        var prepareAllJobStepsResult = await TryPrepareAllJobSteps(jobStepGrainAndRequests);

        if (prepareAllJobStepsResult.TryGetResult(out var preparedAllJobSteps) && !preparedAllJobSteps)
        {
#pragma warning disable CA2201
            _ = await WriteStatusChanged(JobStatus.Failed, new("Failed while preparing job steps. Could not prepare all steps."));
#pragma warning restore CA2201
            return StartJobError.CouldNotPrepareJobSteps;
        }
        _ = await WriteStatusChanged(JobStatus.StartingSteps);

        var startJobStepsResult = await StartAndSubscribeToAllJobSteps(grainId);
        if (startJobStepsResult.TryGetError(out var startJobStepsError))
        {
            if (startJobStepsError != StartJobError.AllJobStepsFailedStarting)
            {
                return startJobStepsError;
            }

            _ = await HandleCompletionResult(await HandleCompletion());
            return startJobStepsError;
        }

        _ = await WriteStatusChanged(JobStatus.Running);
        return Result<StartJobError>.Success();
    }

    async Task<Result<bool, OneOf.Types.None>> TryPrepareAllJobSteps(ReadOnlyDictionary<JobStepId, JobStepGrainAndRequest> jobStepRequests)
    {
        foreach (var (id, grainAndRequest) in jobStepRequests)
        {
            var prepareResult = await grainAndRequest.Grain.Prepare(grainAndRequest.Request);
            if (!prepareResult.TryGetError(out var prepareJobStepError))
            {
                continue;
            }
            _logger.FailedPreparingJobStep(id, prepareJobStepError);
            return false;
        }
        return true;
    }

    async Task<Result<StartJobError>> StartAndSubscribeToAllJobSteps(GrainId grainId)
    {
        var numFailedJobSteps = 0;
        await OnBeforeStartingJobSteps();
        foreach (var idAndGrain in _jobStepGrains!)
        {
            var (id, jobStep) = idAndGrain;
            try
            {
                if (!(await jobStep.Start(grainId)).TryGetError(out var startError))
                {
                    await SubscribeJobStep(_jobStepGrains![id].AsReference<IJobObserver>());
                    continue;
                }
                numFailedJobSteps++;
                _logger.FailedStartingJobStep(id, startError);
            }
            catch (Exception ex)
            {
                numFailedJobSteps++;
                _logger.FailedStartingJobStep(ex, id);
            }
        }
        if (numFailedJobSteps == 0)
        {
            return Result<StartJobError>.Success();
        }
        State.Progress.FailedSteps += numFailedJobSteps;
        return numFailedJobSteps == _jobStepGrains.Count
            ? StartJobError.AllJobStepsFailedStarting
            : StartJobError.FailedStartingSomeJobSteps;
    }

    Task SubscribeJobStep(IJobObserver observer)
    {
        _observers?.Subscribe(observer, observer);
        return Task.CompletedTask;
    }
    Task UnsubscribeJobStep(IJobObserver observer)
    {
        _observers?.Unsubscribe(observer);
        return Task.CompletedTask;
    }
}
