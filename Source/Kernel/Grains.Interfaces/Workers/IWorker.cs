// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Workers;

/// <summary>
/// Defines a worker for a long running process.
/// </summary>
/// <typeparam name="TRequest">Type of request.</typeparam>
/// <typeparam name="TResult">Type of response.</typeparam>
public interface IWorker<TRequest, TResult> : IGrainWithGuidKey
{
    /// <summary>
    /// Gets the status of the worker.
    /// </summary>
    /// <returns><see cref="WorkerStatus"/> of the worker.</returns>
    Task<WorkerStatus> GetStatus();

    /// <summary>
    /// Gets the result of the worker.
    /// </summary>
    /// <returns>The result.</returns>
    Task<TResult> GetResult();

    /// <summary>
    /// Gets the exception that occurred during the execution of the worker.
    /// </summary>
    /// <returns>Exception that occurred.</returns>
    Task<Exception> GetException();

    /// <summary>
    /// Starts the worker.
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(TRequest request);
}
