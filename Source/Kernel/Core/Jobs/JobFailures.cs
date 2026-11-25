// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Reports the jobs that failed from the hooks a job overrides.
/// </summary>
/// <remarks>
/// Choreography has no metric of its own for a job that ends with failures, so the jobs of the kernel report it from
/// the OnAllStepsCompleted and OnFailedToPrepare hooks. A job removed while it still has steps left counts only if a
/// step had already failed.
/// </remarks>
internal static class JobFailures
{
    /// <summary>
    /// Reports a job whose steps have all finished, when some of them failed.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> of the job.</param>
    /// <param name="jobType">The type of the job, in lowercase with underscores.</param>
    /// <param name="progress">The <see cref="JobProgress"/> of the job.</param>
    internal static void Completed(IServiceProvider services, string jobType, JobProgress progress)
    {
        if (progress.HasFailures)
        {
            services.GetService<IJobFailureMetrics>()?.Failed(jobType);
        }
    }

    /// <summary>
    /// Reports a job that could not prepare its steps.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> of the job.</param>
    /// <param name="jobType">The type of the job, in lowercase with underscores.</param>
    internal static void FailedToPrepare(IServiceProvider services, string jobType) =>
        services.GetService<IJobFailureMetrics>()?.Failed(jobType);
}
