// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Workers;

/// <summary>
/// Provides a means of Starting long running work, polling said work, and retrieving an eventual result/exception.
/// </summary>
/// <remarks>
/// Based on the work done here: https://github.com/OrleansContrib/Orleans.SyncWork.
/// </remarks>
public interface ICpuBoundWorker : IGrainWithGuidKey
{
    /// <summary>
    /// Gets the long running work status.
    /// </summary>
    /// <returns>The status of the long running work.</returns>
    Task<CpuBoundWorkerStatus> GetWorkStatus();

    /// <summary>
    /// Gets the exception information when the long running work faulted.
    /// </summary>
    /// <returns>Possibly the exception information as it relates to the failure. Can be null.</returns>
    Task<Exception?> GetException();
}

/// <summary>
/// Provides a means of Starting long running work, polling said work, and retrieving an eventual result/exception.
/// </summary>
/// <typeparam name="TRequest">The type of request to dispatch.</typeparam>
/// <typeparam name="TResult">The type of result to receive.</typeparam>
/// <remarks>
/// Based on the work done here: https://github.com/OrleansContrib/Orleans.SyncWork.
/// </remarks>
public interface ICpuBoundWorker<in TRequest, TResult> : ICpuBoundWorker
{
    /// <summary>
    /// The result of the long running work.
    /// </summary>
    /// <returns>Possible the result of the work done through the SyncWorker. Can be null.</returns>
    Task<TResult?> GetResult();
}
