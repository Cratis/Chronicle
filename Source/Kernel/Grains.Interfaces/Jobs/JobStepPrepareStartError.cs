// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// The type of error that occurred while performing start operation on <see cref="IJobStep"/>.
/// </summary>
public enum JobStepPrepareStartError
{
    /// <summary>
    /// Unknown error occurred.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The job step was not prepared before it started.
    /// </summary>
    NotPrepared = 1,

    /// <summary>
    /// The request type was wrong.
    /// </summary>
    WrongRequestType = 2,

    /// <summary>
    /// Failed to persist internal state.
    /// </summary>
    FailedPersistingState = 3
}