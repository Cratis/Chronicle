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
        var step = await _steps.Reader.ReadAsync(cancellationToken);
        return step();
    }

    /// <inheritdoc/>
    public void Dispose() => _disposed.TrySetResult();
}
