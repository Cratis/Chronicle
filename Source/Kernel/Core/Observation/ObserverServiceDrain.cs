// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Coordinates admission and joining of observer service work.
/// </summary>
internal class ObserverServiceDrain
{
    readonly object _lock = new();
    readonly HashSet<(ObserverDetails Observer, bool Replay)> _windows = [];
    readonly HashSet<Task> _inFlight = [];
    bool _stopping;

    /// <summary>
    /// Admits and tracks a service call and its bulk window.
    /// </summary>
    /// <typeparam name="T">The call's result.</typeparam>
    /// <param name="observer">The observer.</param>
    /// <param name="replay">Whether the window belongs to a replay.</param>
    /// <param name="callback">The work to join at stop.</param>
    /// <returns>The result of the work.</returns>
    /// <exception cref="ObserverServiceStopping">The service no longer accepts work.</exception>
    internal async Task<T> Run<T>(ObserverDetails observer, bool replay, Func<Task<T>> callback)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock)
        {
            if (_stopping)
            {
                throw new ObserverServiceStopping();
            }

            _windows.Add((observer, replay));
            _inFlight.Add(completion.Task);
        }

        try
        {
            return await callback();
        }
        finally
        {
            lock (_lock)
            {
                _inFlight.Remove(completion.Task);
                completion.SetResult();
            }
        }
    }

    /// <summary>
    /// Refuses new work and ends the windows within the shared deadline.
    /// </summary>
    /// <param name="endWindow">The operation ending each local window.</param>
    /// <param name="cancellationToken">The shared stop deadline.</param>
    /// <returns>The stop operation.</returns>
    internal async Task Stop(Func<ObserverDetails, bool, CancellationToken, Task> endWindow, CancellationToken cancellationToken)
    {
        Task[] work;
        lock (_lock)
        {
            _stopping = true;
            work = _inFlight.ToArray();
        }

        try
        {
            await Task.WhenAll(work).WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Still end the windows with the canceled token: this cancels their in-flight driver writes.
        }

        (ObserverDetails Observer, bool Replay)[] windows;
        lock (_lock)
        {
            windows = _windows.ToArray();
        }

        try
        {
            await Task.WhenAll(windows.Select(window => endWindow(window.Observer, window.Replay, cancellationToken))).WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The window drain owns error logging; no further time remains to join it.
        }
    }

    /// <summary>
    /// Forgets a window only after its ordinary finalization succeeds.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <param name="replay">Whether this is a replay window.</param>
    internal void Ended(ObserverDetails observer, bool replay)
    {
        lock (_lock)
        {
            _windows.Remove((observer, replay));
        }
    }
}
