// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// The type of errors that can occur when removing a Job.
/// </summary>
public enum RemoveJobError
{
    /// <summary>
    /// Unknown error.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Job has been completed some way and cannot be removed.
    /// </summary>
    JobIsCompleted = 1,

    /// <summary>
    /// Job is already being removed.
    /// </summary>
    JobIsAlreadyBeingRemoved = 2,
}
