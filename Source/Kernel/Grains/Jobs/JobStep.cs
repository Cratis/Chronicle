// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
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
    }

    /// <inheritdoc/>
    public Task Stop() => throw new NotImplementedException();

    /// <summary>
    /// The method that gets called when the step should do its work.
    /// </summary>
    /// <param name="request">The request object for the step.</param>
    /// <returns>Awaitable task</returns>
    protected abstract Task PerformStep(TRequest request);

    /// <inheritdoc/>
    protected override async Task<object> PerformWork(TRequest request)
    {
        await PerformStep(request);
        return "OK";
    }
}
