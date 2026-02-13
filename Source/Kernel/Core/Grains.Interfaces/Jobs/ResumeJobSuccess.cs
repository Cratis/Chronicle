// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// The type of success that happens when trying to resume a job.
/// </summary>
public enum ResumeJobSuccess
{
    /// <summary>
    /// Successful resume.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Job cannot be resumed.
    /// </summary>
    JobIsCompleted = 1,

    /// <summary>
    /// Job cannot be resumed because it is already running.
    /// </summary>
    JobAlreadyRunning = 2,
}
