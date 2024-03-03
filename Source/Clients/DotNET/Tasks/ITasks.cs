// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Tasks;

/// <summary>
/// Defines a factory for tasks.
/// </summary>
public interface ITasks
{
    /// <summary>
    /// Run a function in a task.
    /// </summary>
    /// <param name="function">Function to run.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work if it has not yet started.</param>
    /// <returns>A task for the function.</returns>
    Task Run(Func<Task> function, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a task that completes after a specified number of milliseconds.
    /// </summary>
    /// <param name="milliseconds">The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work if it has not yet started.</param>
    /// <returns>A task that represents the time delay.</returns>
    Task Delay(int milliseconds, CancellationToken cancellationToken = default);
}
