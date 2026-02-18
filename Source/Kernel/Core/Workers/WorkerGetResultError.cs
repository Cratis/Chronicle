// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Workers;

/// <summary>
/// The reason for not getting <see cref="IGrainWithBackgroundTask{TRequest,TResult}"/> result.
/// </summary>
public enum WorkerGetResultError
{
    /// <summary>
    /// Work has not started.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Work has not finished.
    /// </summary>
    NotFinished = 1,

    /// <summary>
    /// Work has been cancelled.
    /// </summary>
    WorkCancelled = 2,
}
