// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// The type of error that occurred while starting <see cref="IJob{TRequest}"/>.
/// </summary>
public enum StartJobError
{
    /// <summary>
    /// Unknown error occurred.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Some job steps failed to prepare.
    /// </summary>
    CouldNotPrepareJobSteps = 1,

    /// <summary>
    /// Some jobs failed to start.
    /// </summary>
    FailedStartingSomeJobSteps = 2,

    /// <summary>
    /// None of the jobs was started.
    /// </summary>
    AllJobStepsFailedStarting = 3,

    /// <summary>
    /// There were no prepared job steps.
    /// </summary>
    NoJobStepsToStart = 4,

    /// <summary>
    /// The job has already been started and successfully been prepared before.
    /// </summary>
    AlreadyBeenPrepared = 5,

    /// <summary>
    /// The job has already completed and cannot be started again.
    /// </summary>
    AlreadyCompleted = 6,
}