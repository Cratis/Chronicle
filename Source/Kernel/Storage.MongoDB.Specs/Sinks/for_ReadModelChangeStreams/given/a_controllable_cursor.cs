// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.given;

/// <summary>
/// A change stream cursor that reports a change or fails only when the spec tells it to, and otherwise waits the way
/// an awaiting getMore does.
/// </summary>
public sealed class a_controllable_cursor : IChangeStreamCursor<BsonDocument>
{
    readonly Channel<Func<bool>> _steps = Channel.CreateUnbounded<Func<bool>>();
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);

    readonly Lock _lock = new();
    readonly List<(int Count, TaskCompletionSource Reached)> _waiters = [];
    int _moves;
    readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc/>
    public IEnumerable<BsonDocument> Current { get; private set; } = [];

    /// <summary>
    /// Gets a task completing when the cursor has been disposed.
    /// </summary>
    public Task Disposed => _disposed.Task;

    /// <summary>
    /// Report one change on the next batch.
    /// </summary>
    public void Report() => _steps.Writer.TryWrite(() =>
    {
        Current = [new BsonDocument("_id", 1)];
        return true;
    });

    /// <summary>
    /// End the stream at the next batch, the way the server does when it invalidates a change stream.
    /// </summary>
    public void End() => _steps.Writer.TryWrite(() => false);

    /// <summary>
    /// Wait until the change stream loop has asked for the given number of batches. The loop asks for the next one only
    /// after it has dealt with the previous, so this is the signal that every batch before it has been processed.
    /// </summary>
    /// <param name="count">The number of requests for a batch to wait for.</param>
    /// <returns>A <see cref="Task"/> completing once the batches have been asked for, failing when they are not before the deadline.</returns>
    public Task WaitForMoves(int count)
    {
        lock (_lock)
        {
            if (_moves >= count)
            {
                return Task.CompletedTask;
            }

            var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((count, reached));
            return reached.Task.WaitAsync(_deadline);
        }
    }

    /// <summary>
    /// Fail the next batch with the given exception.
    /// </summary>
    /// <param name="exception">The exception to fail with.</param>
    public void Fail(Exception exception) => _steps.Writer.TryWrite(() => throw exception);

    /// <inheritdoc/>
    public BsonDocument GetResumeToken() => [];

    /// <inheritdoc/>
    public bool MoveNext(CancellationToken cancellationToken = default) => MoveNextAsync(cancellationToken).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _moves++;
            foreach (var (_, reached) in _waiters.Where(waiter => waiter.Count <= _moves))
            {
                reached.TrySetResult();
            }
        }

        var step = await _steps.Reader.ReadAsync(cancellationToken);
        return step();
    }

    /// <inheritdoc/>
    public void Dispose() => _disposed.TrySetResult();
}
