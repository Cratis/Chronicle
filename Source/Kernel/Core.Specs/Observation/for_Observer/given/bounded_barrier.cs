// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// A barrier a spec waits on with a bound, so a path that never reaches it fails the spec instead of hanging or passing.
/// </summary>
public class bounded_barrier
{
    static readonly TimeSpan _bound = TimeSpan.FromSeconds(10);
    readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Called by the code under test: signals arrival and waits to be released.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task Arrive()
    {
        _reached.TrySetResult();
        return _released.Task;
    }

    /// <summary>
    /// Waits for the code under test to arrive; throws a <see cref="TimeoutException"/> if it never does.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task WaitUntilReached() => _reached.Task.WaitAsync(_bound);

    /// <summary>
    /// Lets the code under test continue.
    /// </summary>
    public void Release() => _released.TrySetResult();

    /// <summary>
    /// Waits for a task that must finish once released; throws a <see cref="TimeoutException"/> if it does not.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <returns>Awaitable task.</returns>
    public static Task Finishes(Task task) => task.WaitAsync(_bound);
}
