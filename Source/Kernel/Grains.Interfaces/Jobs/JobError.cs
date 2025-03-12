// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// The type of error that can occur when performing an operation on a <see cref="IJob"/>.
/// </summary>
public enum JobError
{
    /// <summary>
    /// The job is in a stopped or completed state.
    /// </summary>
    JobIsStoppedOrCompleted = 0,

    /// <summary>
    /// An unknown error occurred.
    /// </summary>
    UnknownError = 1,

    /// <summary>
    /// A storage related error occurred.
    /// </summary>
    StorageError = 2,

    /// <summary>
    /// Failed to persist state.
    /// </summary>
    PersistStateError = 3,

    /// <summary>
    /// Unknown job step.
    /// </summary>
    UnknownJobStep = 4
}
