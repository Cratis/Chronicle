// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// The type of success that happens when trying to resume a job.
/// </summary>
public enum ResumeJobSuccess
{
    Success = 0,
    JobCannotBeResumed = 1,
    JobAlreadyRunning = 2,
}
