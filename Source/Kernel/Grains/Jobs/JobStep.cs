// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Grains.Workers;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStep{TRequest, TResult}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request for the step.</typeparam>
/// <typeparam name="TResult">Type of result for the step.</typeparam>
/// <typeparam name="TState">Type of state for the step.</typeparam>
public abstract class JobStep<TRequest, TResult, TState> : CpuBoundWorker<TRequest, JobStepResult>, IJobStep<TRequest, TResult>, IJobObserver, IDisposable
    where TState : JobStepState
{
    readonly IPersistentState<TState> _state;
    readonly CancellationTokenSource _cancellationTokenSource = new();
    bool _running;
    JobStepResult? _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStep{TRequest, TResult, TState}"/> class.
    /// </summary>
    /// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
    protected JobStep(
        [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
        IPersistentState<TState> state)
    {
        Job = new NullJob();
        ThisJobStep = null!;
        _state = state;
    }

    /// <summary>
    /// Gets the <see cref="JobStepId"/> for this job step.
    /// </summary>
    public JobStepId JobStepId => this.GetPrimaryKey();

    /// <summary>
    /// Gets the parent job.
    /// </summary>
    protected IJob Job { get; private set; }

    /// <summary>
    /// Gets the job step as a Grain reference.
    /// </summary>
    protected IJobStep<TRequest, TResult> ThisJobStep { get; private set; }

    /// <summary>
    /// Gets the state for the job step.
    /// </summary>
    protected TState State => _state.State;

    /// <inheritdoc/>
    public void Dispose() => _cancellationTokenSource.Dispose();

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.MaxValue);

        await base.OnActivateAsync(cancellationToken);

        _ = this.GetPrimaryKey(out var key);
        var jobStepKey = (JobStepKey)key;

        var grainType = this.GetGrainType();
        _state.State.Name = GetType().Name;
        _state.State.Id = new(jobStepKey.JobId, JobStepId);
        _state.State.Type = grainType;
    }

    /// <inheritdoc/>
    public async Task Start(GrainId jobId, TRequest request)
    {
        _running = true;
        Job = GrainFactory.GetGrain(jobId).AsReference<IJob>();
        ThisJobStep = GrainFactory.GetGrain(GrainReference.GrainId).AsReference<IJobStep<TRequest, TResult>>();

        StatusChanged(JobStepStatus.Running);
        _state.State.Request = request!;
        await _state.WriteStateAsync();
        await Prepare(request);
        await Start(request, _cancellationTokenSource.Token);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Pause()
    {
        _cancellationTokenSource.Cancel();
        await Job.Unsubscribe(this.AsReference<IJobObserver>());
        StatusChanged(JobStepStatus.Paused);
    }

    /// <inheritdoc/>
    public Task Resume(GrainId grainId)
    {
        if (_running)
        {
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Stop()
    {
        _cancellationTokenSource.Cancel();
        await Job.Unsubscribe(this.AsReference<IJobObserver>());
        StatusChanged(JobStepStatus.Stopped);
        await _state.WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task ReportStatusChange(JobStepStatus status)
    {
        StatusChanged(status);
        await _state.WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task ReportFailure(IList<string> exceptionMessages, string exceptionStackTrace)
    {
        StatusChanged(JobStepStatus.Failed, exceptionMessages, exceptionStackTrace);
        await _state.WriteStateAsync();
        await Job.OnStepFailed(JobStepId, _result!);
    }

    /// <summary>
    /// Prepare the step before it starts.
    /// </summary>
    /// <param name="request">The request object for the step.</param>
    /// <returns>Awaitable task.</returns>
    public virtual Task Prepare(TRequest request) => Task.CompletedTask;

    /// <inheritdoc/>
    public void OnJobStopped() => _cancellationTokenSource.Cancel();

    /// <inheritdoc/>
    public void OnJobPaused() => _cancellationTokenSource.Cancel();

    /// <summary>
    /// The method that gets called when the step should do its work.
    /// </summary>
    /// <param name="request">The request object for the step.</param>
    /// <param name="cancellationToken">Cancellation token that can cancel the step work.</param>
    /// <returns>True if successful, false if not.</returns>
    protected abstract Task<JobStepResult> PerformStep(TRequest request, CancellationToken cancellationToken);

    /// <inheritdoc/>
    protected override async Task<JobStepResult> PerformWork(TRequest request)
    {
        await ThisJobStep.ReportStatusChange(JobStepStatus.Running);

        var result = JobStepResult.Succeeded();
        try
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                result = await PerformStep(request, _cancellationTokenSource.Token);
            }
        }
        catch (Exception ex)
        {
            result = new JobStepResult(JobStepStatus.Failed, ex.GetAllMessages(), ex.StackTrace ?? string.Empty);
        }

        _result = result;
        if (result.IsSuccess)
        {
            await Job.OnStepSucceeded(JobStepId, result);
            await ThisJobStep.ReportStatusChange(JobStepStatus.Succeeded);
        }
        else
        {
            await ThisJobStep.ReportFailure(result.Messages.ToList(), result.ExceptionStackTrace);
        }

        DeactivateOnIdle();

        return result;
    }

    void StatusChanged(JobStepStatus status, IEnumerable<string>? exceptionMessages = null!, string? exceptionStackTrace = null!)
    {
        _state.State.StatusChanges.Add(new JobStepStatusChanged
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow,
            ExceptionMessages = exceptionMessages ?? Enumerable.Empty<string>(),
            ExceptionStackTrace = exceptionStackTrace ?? string.Empty
        });
        _state.State.Status = status;
    }
}
