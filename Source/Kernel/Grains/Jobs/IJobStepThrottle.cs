// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Defines a system that throttles parallel execution of job steps.
/// </summary>
public interface IJobStepThrottle
{
    /// <summary>
    /// Acquire a slot for executing a job step.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task AcquireAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Release a slot after job step execution completes.
    /// </summary>
    void Release();
}
