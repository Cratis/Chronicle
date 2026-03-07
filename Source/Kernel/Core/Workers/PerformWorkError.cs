// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Workers;

/// <summary>
/// The type of the error that occurred while performing work step.
/// </summary>
public enum PerformWorkError
{
    /// <summary>
    /// None error occurred.
    /// </summary>
    None = 0,

    /// <summary>
    /// The work was cancelled.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// An error occurred in the worker itself.
    /// </summary>
    WorkerError = 2,

    /// <summary>
    /// An unexpected error occurred while performing the actual work.
    /// </summary>
    PerformingWorkError = 3,
}