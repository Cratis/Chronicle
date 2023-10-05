// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.SyncWork;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStep{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request for the step.</typeparam>
public abstract class JobStep<TRequest> : SyncWorker<TRequest, object>, IJobStep<TRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobStep{TRequest}"/> class.
    /// </summary>
    /// <param name="taskScheduler"><see cref="LimitedConcurrencyLevelTaskScheduler"/> to use for scheduling.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    protected JobStep(LimitedConcurrencyLevelTaskScheduler taskScheduler, ILogger logger) : base(logger, taskScheduler)
    {
        Job = new NullJob();
    }

    /// <summary>
    /// Gets the <see cref="JobStepId"/> for this job step.
    /// </summary>
    public JobStepId JobStepId => this.GetPrimaryKey();

    /// <summary>
    /// Gets the parent job.
    /// </summary>
    protected IJob Job { get; private set; }

    /// <inheritdoc/>
    public async Task Start(GrainId jobId, TRequest request)
    {
        Job = (GrainFactory.GetGrain(jobId) as IJob)!;
        await PrepareStep(request);
        await Start(request);
    }

    /// <inheritdoc/>
    public Task Stop() => throw new NotImplementedException();

    /// <summary>
    /// Prepare the step before it starts.
    /// </summary>
    /// <param name="request">The request object for the step.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task PrepareStep(TRequest request) => Task.CompletedTask;

    /// <summary>
    /// The method that gets called when the step should do its work.
    /// </summary>
    /// <param name="request">The request object for the step.</param>
    /// <returns>True if successful, false if not.</returns>
    protected abstract Task<bool> PerformStep(TRequest request);

    /// <inheritdoc/>
    protected override async Task<object> PerformWork(TRequest request)
    {
        var result = await PerformStep(request);
        if (result)
        {
            await Job.OnStepSuccessful(JobStepId);
        }
        else
        {
            await Job.OnStepFailed(JobStepId);
        }

        return "OK";
    }
}
