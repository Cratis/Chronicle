// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents the status of a <see cref="IJob"/>.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// The job has been created.
    /// </summary>
    Created = 1,

    /// <summary>
    /// The job has been started.
    /// </summary>
    Started = 2,

    /// <summary>
    /// The job has been completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The job has completed with failure.
    /// </summary>
    CompletedWithFailure = 4,

    /// <summary>
    /// The job has been cancelled.
    /// </summary>
    Cancelled = 5
}
