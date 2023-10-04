// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.SyncWork;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents a step in a job.
/// </summary>
/// <typeparam name="TRequest">Type of the request for the job.</typeparam>
public interface IJobStep<TRequest> : ISyncWorker<TRequest, object>
{
    /// <summary>
    /// Stop the job step.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Stop();
}
