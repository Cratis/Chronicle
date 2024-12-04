// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// The different success results for performing <see cref="IJobStep.Resume"/>.
/// </summary>
public enum JobStepResumeSuccess
{
    /// <summary>
    /// Successfully resumed the <see cref="IJobStep"/>.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The <see cref="IJobStep"/> is already running, no need to resume.
    /// </summary>
    AlreadyRunning = 1
}
