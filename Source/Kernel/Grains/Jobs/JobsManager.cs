// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobsManager"/>.
/// </summary>
public class JobsManager : Grain, IJobsManager
{
    /// <inheritdoc/>
    public Task Start<TJob, TRequest>(JobId jobId, TRequest request)
        where TJob : IJob => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task OnCompleted(JobId jobId) => throw new NotImplementedException();
}
