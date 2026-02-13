// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStepThrottle"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobStepThrottle"/> class.
/// </remarks>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">Logger for logging.</param>
public class JobStepThrottle(IOptions<ChronicleOptions> options, ILogger<JobStepThrottle> logger) : IJobStepThrottle, IDisposable
{
    readonly SemaphoreSlim _semaphore = CreateSemaphore(options.Value.Jobs.GetEffectiveMaxParallelSteps());

    /// <inheritdoc/>
    public Task AcquireAsync(CancellationToken cancellationToken = default)
    {
        logger.AcquiringJobStepSlot();
        return _semaphore.WaitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Release()
    {
        logger.ReleasingJobStepSlot();
        _semaphore.Release();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _semaphore.Dispose();
    }

    static SemaphoreSlim CreateSemaphore(int maxParallelSteps) => new(maxParallelSteps, maxParallelSteps);
}
