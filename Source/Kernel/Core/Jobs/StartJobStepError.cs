// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// The type of error that occurred while performing start operation on <see cref="IJobStep"/>.
/// </summary>
public enum StartJobStepError
{
    /// <summary>
    /// Unknown error occurred.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The request type was wrong.
    /// </summary>
    NotPrepared = 1,

    /// <summary>
    /// The job step is already started.
    /// </summary>
    AlreadyStarted = 2,

    /// <summary>
    /// The job step is in a completed state.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The job step is in an unrecoverable failed state.
    /// </summary>
    UnrecoverableFailedState = 4,
}