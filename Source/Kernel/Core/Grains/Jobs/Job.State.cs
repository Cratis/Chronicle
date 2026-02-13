// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
namespace Cratis.Chronicle.Grains.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    /// <summary>
    /// Writes persisted state.
    /// </summary>
    /// <returns>A task returning <see cref="Catch"/>.</returns>
    protected async Task<Catch> WriteState()
    {
        _logger.BeginJobScope(JobId, JobKey);
        try
        {
            await WriteStateAsync();
            return Catch.Success();
        }
        catch (Exception e)
        {
            _logger.FailedWritingState(e);
            return e;
        }
    }

    async Task<Result<JobError>> SetTotalSteps(int totalSteps)
    {
        _logger.BeginJobScope(JobId, JobKey);
        try
        {
            State.Progress.TotalSteps = totalSteps;
            var writeResult = await WriteState();
            return writeResult.Match(_ => Result.Success<JobError>(), ex =>
            {
                _logger.FailedToSetTotalSteps(ex);
                return JobError.PersistStateError;
            });
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return JobError.UnknownError;
        }
    }

    async Task<Result<JobError>> WriteStatusChanged(JobStatus status, Exception? exception = null)
    {
        _logger.BeginJobScope(JobId, JobKey);
        try
        {
            _logger.ChangingStatus(status);
            StatusChanged(status, exception);
            var writeStateResult = await WriteState();
            return writeStateResult.Match(_ => Result.Success<JobError>(), _ =>
            {
                _logger.FailedWritingStatusChange(status);
                return JobError.PersistStateError;
            });
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return JobError.UnknownError;
        }
    }

    bool JobIsRunning() => State.Status is JobStatus.Running or JobStatus.PreparingJob or JobStatus.PreparingSteps or JobStatus.StartingSteps;

    bool JobIsPrepared() => State.StatusChanges.Any(change => change.Status is JobStatus.StartingSteps);

    void StatusChanged(JobStatus status, Exception? exception = null)
    {
        State.StatusChanges.Add(new()
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow,
            ExceptionStackTrace = exception?.StackTrace ?? string.Empty,
            ExceptionMessages = exception?.GetAllMessages() ?? []
        });
        State.Status = status;
    }
}
