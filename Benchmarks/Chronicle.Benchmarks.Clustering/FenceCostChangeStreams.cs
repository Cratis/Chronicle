// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Holds a number of change streams open on a collection and drains them, the way served read-model observers do.
/// </summary>
internal sealed class FenceCostChangeStreams : IAsyncDisposable
{
    readonly CancellationTokenSource _stop = new();
    readonly List<Task> _readers = [];
    long _delivered;

    /// <summary>
    /// Gets the number of change events delivered so far.
    /// </summary>
    internal long Delivered => Interlocked.Read(ref _delivered);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        await Task.WhenAll(_readers.Select(reader => reader.ContinueWith(_ => { }, TaskScheduler.Default)));
        _stop.Dispose();
    }

    /// <summary>
    /// Opens the change streams.
    /// </summary>
    /// <param name="collection">The collection to watch.</param>
    /// <param name="count">How many streams to open.</param>
    /// <returns>The open streams.</returns>
    internal static async Task<FenceCostChangeStreams> Open(IMongoCollection<BsonDocument> collection, int count)
    {
        var streams = new FenceCostChangeStreams();
        var cursors = await Task.WhenAll(Enumerable.Range(0, count).Select(_ => collection.WatchAsync(new ChangeStreamOptions { MaxAwaitTime = TimeSpan.FromMilliseconds(500) })));
        streams._readers.AddRange(cursors.Select(streams.Drain));
        return streams;
    }

    async Task Drain(IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor)
    {
        using (cursor)
        {
            while (!_stop.IsCancellationRequested && await cursor.MoveNextAsync(_stop.Token))
            {
                Interlocked.Add(ref _delivered, cursor.Current.Count());
            }
        }
    }
}
