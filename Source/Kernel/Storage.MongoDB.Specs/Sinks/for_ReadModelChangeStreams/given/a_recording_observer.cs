// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.given;

/// <summary>
/// An observer recording what it receives, and letting a spec await a number of values or an error.
/// </summary>
/// <typeparam name="T">Type of value observed.</typeparam>
public sealed class a_recording_observer<T> : IObserver<T>
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);

    readonly Lock _lock = new();
    readonly List<T> _values = [];
    readonly List<(int Count, TaskCompletionSource Reached)> _waiters = [];
    readonly TaskCompletionSource<Exception> _error = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the values received so far.
    /// </summary>
    public IReadOnlyList<T> Values
    {
        get
        {
            lock (_lock)
            {
                return [.. _values];
            }
        }
    }

    /// <summary>
    /// Gets whether an error has been received.
    /// </summary>
    public bool HasFailed => _error.Task.IsCompleted;

    /// <summary>
    /// Wait for the error, failing when none arrives before the deadline.
    /// </summary>
    /// <returns>The error received.</returns>
    public Task<Exception> WaitForError() => _error.Task.WaitAsync(_deadline);

    /// <summary>
    /// Wait until at least the given number of values have been received, failing when they do not arrive before the deadline.
    /// </summary>
    /// <param name="count">The number of values to wait for.</param>
    /// <returns>A <see cref="Task"/> completing once the values have been received.</returns>
    public Task WaitForValues(int count)
    {
        lock (_lock)
        {
            if (_values.Count >= count)
            {
                return Task.CompletedTask;
            }

            var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((count, reached));
            return reached.Task.WaitAsync(_deadline);
        }
    }

    /// <inheritdoc/>
    public void OnNext(T value)
    {
        lock (_lock)
        {
            _values.Add(value);
            foreach (var (_, reached) in _waiters.Where(waiter => waiter.Count <= _values.Count))
            {
                reached.TrySetResult();
            }
        }
    }

    /// <inheritdoc/>
    public void OnError(Exception error)
    {
        _error.TrySetResult(error);
        lock (_lock)
        {
            foreach (var (_, reached) in _waiters)
            {
                reached.TrySetException(error);
            }
        }
    }

    /// <inheritdoc/>
    public void OnCompleted()
    {
    }
}
