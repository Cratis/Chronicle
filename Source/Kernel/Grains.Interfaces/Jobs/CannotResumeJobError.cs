// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Error types for when a job cannot be resumed.
/// </summary>
public enum CannotResumeJobError
{
    /// <summary>
    /// Unknown error.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Job has not been prepared and started beforehand.
    /// </summary>
    JobIsNotPrepared = 1,

    /// <summary>
    /// Job cannot be resumed.
    /// </summary>
    JobCannotBeResumed = 2,
}
