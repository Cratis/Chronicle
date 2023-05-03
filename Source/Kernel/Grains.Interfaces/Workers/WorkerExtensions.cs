// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Workers;

/// <summary>
/// Extension methods for working with <see cref="IWorker{TRequest, TResult}"/>.
/// </summary>
public static class WorkerExtensions
{
    /// <summary>
    /// Wait for the worker to complete and return the result.
    /// </summary>
    /// <param name="worker">Worker to wait for.</param>
    /// <param name="millisecondsBetweenPolls">Number of milliseconds between polls. Defaults to 100.</param>
    /// <typeparam name="TRequest">Type of request.</typeparam>
    /// <typeparam name="TResult">Type of result.</typeparam>
    /// <returns>The result</returns>
    /// <remarks>
    /// If an exception is thrown in the worker, this will bubble up and be thrown from this method.
    /// </remarks>
    public static async Task<TResult> WaitForResult<TRequest, TResult>(this IWorker<TRequest, TResult> worker, int millisecondsBetweenPolls = 100)
    {
        var status = WorkerStatus.Idle;

        while (status != WorkerStatus.Completed && status != WorkerStatus.Failed)
        {
            status = await worker.GetStatus();
            await Task.Delay(millisecondsBetweenPolls);
        }

        if (status == WorkerStatus.Failed)
        {
            throw await worker.GetException();
        }

        return await worker.GetResult();
    }
}
