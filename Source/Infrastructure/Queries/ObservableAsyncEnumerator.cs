// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Queries;

/// <summary>
/// Represents an <see cref="IAsyncEnumerator{T}"/> for a <see cref="ClientObservable{T}"/>.
/// </summary>
/// <typeparam name="T">Type the enumerator is for.</typeparam>
public class ObservableAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    readonly IDisposable _subscriber;
    readonly CancellationToken _cancellationToken;
    readonly ConcurrentQueue<T> _items = new();
    TaskCompletionSource _taskCompletionSource = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableAsyncEnumerator{T}"/> class.
    /// </summary>
    /// <param name="observable">The observable to return from.</param>
    /// <param name="cancellationToken">Cancellation token for cancelling any enumeration.</param>
    public ObservableAsyncEnumerator(IObservable<T> observable, CancellationToken cancellationToken)
    {
        Current = default!;
        _subscriber = observable.Subscribe(_ =>
        {
            _items.Enqueue(_);
            if (!_taskCompletionSource.Task.IsCompletedSuccessfully)
            {
                _taskCompletionSource?.SetResult();
            }
        });
        _cancellationToken = cancellationToken;
    }

    /// <inheritdoc/>
    public T Current { get; private set; }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _subscriber.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MoveNextAsync()
    {
        if (_cancellationToken.IsCancellationRequested) return false;
        await _taskCompletionSource.Task;
        _items.TryDequeue(out var item);
        Current = item!;
        _taskCompletionSource = new();
        if (!_items.IsEmpty)
        {
            _taskCompletionSource.SetResult();
        }

        return true;
    }
}
