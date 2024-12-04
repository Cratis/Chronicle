// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using OneOf.Types;
namespace Cratis.Chronicle.Grains.Workers;

/// <summary>
/// Provides a means of Starting long-running work, polling said work, and retrieving an eventual result/exception.
/// </summary>
/// <remarks>
/// Based on the work done here: https://github.com/OrleansContrib/Orleans.SyncWork.
/// </remarks>
public interface ICpuBoundWorker : IGrainWithGuidKey
{
    /// <summary>
    /// Gets the long-running work status.
    /// </summary>
    /// <returns>The status of the long-running work.</returns>
    Task<CpuBoundWorkerStatus> GetWorkStatus();

    /// <summary>
    /// Gets the exception information when the long-running work faulted.
    /// </summary>
    /// <returns>Possibly the exception information as it relates to the failure. Can be null.</returns>
    Task<Result<None, Exception>> GetException();
}