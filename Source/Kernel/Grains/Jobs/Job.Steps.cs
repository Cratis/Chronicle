// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Reflection;
namespace Cratis.Chronicle.Grains.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    /// <inheritdoc/>
    public async Task<Result<JobError>> OnStepSucceeded(JobStepId stepId, JobStepResult jobStepResult)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            _logger.StepSuccessfullyCompleted(stepId);
            State.Progress.SuccessfulSteps++;
            _ = await WriteState();
            var handleCompletedStepResult = await HandleJobStepCompleted(stepId, jobStepResult);
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

    /// <inheritdoc/>
    public async Task<Result<JobError>> OnStepStopped(JobStepId stepId, JobStepResult jobStepResult)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            _logger.StepStopped(stepId);
            State.Progress.StoppedSteps++;
            _ = await WriteState();
            var handleCompletedStepResult = await HandleJobStepCompleted(stepId, jobStepResult);
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

    /// <inheritdoc/>
    public async Task<Result<JobError>> OnStepFailed(JobStepId stepId, JobStepResult jobStepResult)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            _logger.StepFailed(stepId);
            State.Progress.FailedSteps++;
            _ = await WriteState();
            var handleCompletedStepResult = await HandleJobStepCompleted(stepId, jobStepResult);
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

    /// <summary>
    /// Called when a step has completed.
    /// </summary>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the completed step.</param>
    /// <param name="result"><see cref="JobStepResult"/> for the completed step.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnStepCompleted(JobStepId jobStepId, JobStepResult result) => Task.CompletedTask;

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
    /// Called before preparing steps.
    /// </summary>
    /// <param name="request">The request associated with the job.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnBeforePrepareSteps(TRequest request) => Task.CompletedTask;

    /// <summary>
    /// Start the job.
    /// </summary>
    /// <param name="request">The request associated with the job.</param>
    /// <returns>Collection of <see cref="JobStepDetails"/> .</returns>
    protected abstract Task<IImmutableList<JobStepDetails>> PrepareSteps(TRequest request);

    async Task<Result<JobError>> HandleJobStepCompleted(JobStepId stepId, JobStepResult result)
    {
        try
        {
            await OnStepCompleted(stepId, result);
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
            HandleCompletionSuccess.NotClearedState => true,
            _ => false
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

    async Task<Dictionary<JobStepId, IJobStep>> GetIdAndGrainReferenceToNonCompletedJobSteps()
    {
        var getJobSteps = await Storage.JobSteps.GetForJob(JobId, JobStepStatus.Scheduled, JobStepStatus.Running, JobStepStatus.Paused, JobStepStatus.Unknown);
        getJobSteps.RethrowError();
        var jobSteps = getJobSteps.AsT0;
        return jobSteps.ToDictionary(jobStep => jobStep.Id.JobStepId, GetJobStepGrain);
    }

    IJobStep GetJobStepGrain(JobStepDetails details) => (GrainFactory.GetGrain(details.Type, details.Id, keyExtension: details.Key) as IJobStep)!;
    IJobStep GetJobStepGrain(JobStepState state) => (GrainFactory.GetGrain((Type)state.Type, state.Id.JobStepId, keyExtension: new JobStepKey(state.Id.JobId, JobKey.EventStore, JobKey.Namespace)) as IJobStep)!;

    async Task<Result<StartJobError>> PrepareAndStartRunningAllSteps(TRequest request)
    {
        try
        {
            var grainId = this.GetGrainId();
            await OnBeforePrepareSteps(request);
            var steps = await PrepareSteps(request);
            _ = await SetTotalSteps(steps.Count);
            if (steps.Count == 0)
            {
                _logger.NoJobStepsToStart();
                _ = HandleCompletion();
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
        _logger.PrepareJobStepsForRunning();
        _ = await WriteStatusChanged(JobStatus.PreparingSteps);
        var jobStepGrainAndRequests = CreateGrainsFromJobSteps(jobSteps);
        if (!await TryPrepareAllJobSteps(jobStepGrainAndRequests))
        {
            _ = await WriteStatusChanged(JobStatus.Failed);
            return StartJobError.CouldNotPrepareJobSteps;
        }
        _jobStepGrains = jobStepGrainAndRequests.ToDictionary(_ => _.Key, _ => _.Value.Grain);
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

    async Task<bool> TryPrepareAllJobSteps(ReadOnlyDictionary<JobStepId, JobStepGrainAndRequest> jobStepRequests)
    {
        var prepareAllSteps = jobStepRequests.Select(async idAndGrain =>
        {
            var (id, jobStep) = idAndGrain;
            try
            {
                if (!(await jobStep.Grain.Prepare(jobStep.Request)).TryGetError(out var prepareError))
                {
                    return (JobStepId: id, Result: Result.Success<PrepareJobStepError>());
                }
                _logger.FailedPreparingJobStep(id, prepareError);
                return (JobStepId: id, Result: prepareError);
            }
            catch (Exception ex)
            {
                _logger.ErrorPreparingJobStep(ex, id);
                return (JobStepId: id, Result: Result.Failed(PrepareJobStepError.Unknown));
            }
        });
        var results = await Task.WhenAll(prepareAllSteps);
        return results.All(_ => _.Result.IsSuccess);
    }

    async Task<Result<StartJobError>> StartAndSubscribeToAllJobSteps(GrainId grainId)
    {
        var prepareAllSteps = _jobStepGrains!.Select(async idAndGrain =>
        {
            var (id, jobStep) = idAndGrain;
            try
            {
                if (!(await jobStep.Start(grainId)).TryGetError(out var startError))
                {
                    return (JobStepId: id, Result: Result.Success<StartJobStepError>());
                }
                _logger.FailedStartingJobStep(id, startError);
                return (JobStepId: id, Result: startError);
            }
            catch (Exception ex)
            {
                _logger.FailedStartingJobStep(ex, id);
                return (JobStepId: id, Result: Result.Failed(StartJobStepError.Unknown));
            }
        });
        var results = await Task.WhenAll(prepareAllSteps);
        var numFailedJobSteps = results.Count(finishedTask => !finishedTask.Result.IsSuccess);
        foreach (var idAndJobStep in results.Where(_ => _.Result.IsSuccess))
        {
            await SubscribeJobStep(_jobStepGrains![idAndJobStep.JobStepId].AsReference<IJobObserver>());
        }
        if (numFailedJobSteps == 0)
        {
            return Result<StartJobError>.Success();
        }
        State.Progress.FailedSteps += numFailedJobSteps;
        return numFailedJobSteps == results.Length
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
