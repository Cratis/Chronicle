// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Workers;

/// <summary>
/// The reason for not getting <see cref="ICpuBoundWorker{TRequest,TResult}"/> result.
/// </summary>
public enum CpuBoundWorkerGetResultError
{
    /// <summary>
    /// Work has not started.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Work was cancelled.
    /// </summary>
    WorkCancelled = 1,

    /// <summary>
    /// Error occurred while performing work.
    /// </summary>
    ErrorOccurred = 2
}
