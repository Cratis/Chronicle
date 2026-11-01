// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// A hand-written fake MongoDB collection: it accepts every bulk write, finds nothing and costs a configurable CPU-bound round trip per find.
/// </summary>
/// <remarks>
/// <see cref="IMongoCollection{TDocument}"/> has about a hundred members and its helper base class is not public, so the interface is
/// implemented with a BCL <see cref="DispatchProxy"/> that handles the two calls the sink makes and rejects everything else.
/// No mocking library is involved.
/// </remarks>
public class NoOpMongoCollection : DispatchProxy
{
    static readonly MethodInfo _emptyCursorFactory = typeof(NoOpMongoCollection).GetMethod(nameof(CreateEmptyCursorTask), BindingFlags.NonPublic | BindingFlags.Static)!;
    static readonly CollectionNamespace _collectionNamespace = new(new DatabaseNamespace("benchmark"), "models");

    /// <summary>
    /// Gets or sets the time a find (a cache miss falling through to the server) takes.
    /// </summary>
    public TimeSpan MissRoundTrip { get; set; }

    /// <summary>
    /// Gets the fake as a MongoDB collection.
    /// </summary>
    public IMongoCollection<BsonDocument> Collection => (IMongoCollection<BsonDocument>)(object)this;

    /// <summary>
    /// Creates a fake collection.
    /// </summary>
    /// <param name="missRoundTrip">The time a find takes.</param>
    /// <returns>The fake.</returns>
    public static NoOpMongoCollection Create(TimeSpan missRoundTrip)
    {
        var proxy = Create<IMongoCollection<BsonDocument>, NoOpMongoCollection>();
        var fake = (NoOpMongoCollection)(object)proxy;
        fake.MissRoundTrip = missRoundTrip;
        return fake;
    }

    /// <summary>
    /// Busy-waits for <see cref="MissRoundTrip"/>, deterministic CPU time instead of a timer.
    /// </summary>
    public void SimulateRoundTrip()
    {
        var ticks = (long)(MissRoundTrip.TotalSeconds * Stopwatch.Frequency);
        if (ticks <= 0)
        {
            return;
        }

        var end = Stopwatch.GetTimestamp() + ticks;
        while (Stopwatch.GetTimestamp() < end)
        {
        }
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        switch (targetMethod?.Name)
        {
            case "get_CollectionNamespace":
                return _collectionNamespace;
            case nameof(IMongoCollection<BsonDocument>.BulkWriteAsync):
                return Task.FromResult<BulkWriteResult<BsonDocument>>(Acknowledge(args!.OfType<IEnumerable<WriteModel<BsonDocument>>>().First()));
            case nameof(IMongoCollection<BsonDocument>.FindAsync):
                SimulateRoundTrip();
                return _emptyCursorFactory.MakeGenericMethod(targetMethod.GetGenericArguments()).Invoke(null, null);
            default:
                throw new NotSupportedException($"{targetMethod?.Name} is not supported by the benchmark's fake collection.");
        }
    }

    static Task<IAsyncCursor<T>> CreateEmptyCursorTask<T>() => Task.FromResult<IAsyncCursor<T>>(new EmptyCursor<T>());

    static BulkWriteResult<BsonDocument>.Acknowledged Acknowledge(IEnumerable<WriteModel<BsonDocument>> requests)
    {
        var processed = requests.ToList();
        return new BulkWriteResult<BsonDocument>.Acknowledged(processed.Count, 0, 0, 0, 0, processed, []);
    }

    sealed class EmptyCursor<T> : IAsyncCursor<T>
    {
        public IEnumerable<T> Current => [];

        public void Dispose()
        {
        }

        public bool MoveNext(CancellationToken cancellationToken = default) => false;

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
