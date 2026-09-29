// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Threading.Channels;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="IReadModelChangeStreams"/>.
/// </summary>
/// <remarks>
/// <para>
/// An open change stream keeps a pooled connection busy in its awaiting getMore almost all of the time, so one stream
/// per observer would drain the connection pool once enough pages are observed. Every observer of the same collection
/// in the same database therefore shares one stream: it is opened by the first observer, signals every observer to
/// read its own page again after each change, and is closed when the last observer leaves.
/// </para>
/// <para>
/// The stream watches the database, narrowed to the observed collection, rather than the collection itself: a replay
/// promotion renames collections, which would invalidate and end a collection-level stream, whereas the database-level
/// one reports the rename and carries on.
/// </para>
/// <para>
/// A server failover, a killed cursor or a read cut short by a concurrent rename is not the end of an observation.
/// Such a failure is retried with a short, bounded backoff - reopening the stream re-reads every page, so nothing that
/// happened while it was down is missed - and only a failure that persists is reported to the observers.
/// </para>
/// </remarks>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
/// <param name="timeProvider">Optional <see cref="TimeProvider"/> the retry backoff waits on; defaults to <see cref="TimeProvider.System"/>.</param>
[Singleton]
public class ReadModelChangeStreams(ILogger<ReadModelChangeStreams> logger, TimeProvider? timeProvider = null) : IReadModelChangeStreams
{
    /// <summary>
    /// The number of consecutive transient failures after which a read or a change stream gives up.
    /// </summary>
    internal const int MaxConsecutiveFailures = 5;

    static readonly TimeSpan _firstRetryDelay = TimeSpan.FromMilliseconds(100);

    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly Lock _lock = new();
    readonly Dictionary<ContainerKey, ContainerChangeStream> _streams = [];

    /// <inheritdoc/>
    public IObservable<T> Observe<T>(IMongoDatabase database, string containerName, Func<CancellationToken, Task<T>> read) =>
        Observable.Create<T>(async (observer, cancellationToken) =>
        {
            // A single slot that is dropped into when already full: changes arriving while a read runs leave one
            // pending signal behind, answered by one read once the running one is done.
            var signals = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true
            });
            var stream = Join(database, containerName, signals.Writer);
            try
            {
                // Throws the failure the shared stream completes the channel with once it has given up.
                while (await signals.Reader.WaitToReadAsync(cancellationToken))
                {
                    signals.Reader.TryRead(out _);
                    observer.OnNext(await ReadWithRetry(containerName, read, cancellationToken));
                }
            }
            finally
            {
                Leave(stream, signals.Writer);
            }
        });

    /// <summary>
    /// Builds the change stream pipeline reporting every change to the named collection.
    /// </summary>
    /// <param name="containerName">The name of the collection to report changes to.</param>
    /// <returns>The <see cref="PipelineDefinition{TInput, TOutput}"/> for a database-level change stream.</returns>
    /// <remarks>
    /// <para>
    /// A change inside the collection - including dropping it - names it in the namespace field of the change. A
    /// rename names its source there and its target in the destination field, and only a rename INTO the collection
    /// is reported: a replay promotion first renames the collection aside and then renames the rebuilt one into its
    /// place, and answering the first rename would read the collection while it does not exist, handing every
    /// observer an empty page between the old state and the replayed one.
    /// </para>
    /// <para>
    /// A read that is started for another reason inside that window - a subscriber joining an open stream, or a
    /// stream reopening after a failure - can still find the collection missing. Filtering the stream cannot help
    /// there, so the read itself must not report a missing collection as a page; the rename into the collection is
    /// reported and triggers the read that follows.
    /// </para>
    /// <para>
    /// Only the resume token and the operation type are kept of each change: every observer reads its page again
    /// anyway, so shipping the changed documents across would be wasted.
    /// </para>
    /// </remarks>
    internal static PipelineDefinition<ChangeStreamDocument<BsonDocument>, BsonDocument> ChangesTo(string containerName) =>
        new BsonDocumentStagePipelineDefinition<ChangeStreamDocument<BsonDocument>, BsonDocument>(
        [
            new BsonDocument("$match", new BsonDocument("$or", new BsonArray
            {
                new BsonDocument
                {
                    { "operationType", new BsonDocument("$ne", "rename") },
                    { "ns.coll", containerName }
                },
                new BsonDocument
                {
                    { "operationType", "rename" },
                    { "to.coll", containerName }
                }
            })),
            new BsonDocument("$project", new BsonDocument { { "_id", 1 }, { "operationType", 1 } })
        ]);

    static bool IsTransient(Exception exception) => exception is MongoException or TimeoutException;

    static TimeSpan DelayAfter(int failures) => _firstRetryDelay * Math.Pow(2, failures - 1);

    async Task<T> ReadWithRetry<T>(string containerName, Func<CancellationToken, Task<T>> read, CancellationToken cancellationToken)
    {
        for (var failures = 1; ; failures++)
        {
            try
            {
                return await read(cancellationToken);
            }
            catch (Exception exception) when (IsTransient(exception) && failures < MaxConsecutiveFailures && !cancellationToken.IsCancellationRequested)
            {
                logger.ReadingObservedCollectionFailed(containerName, failures, exception);
                await Task.Delay(DelayAfter(failures), _timeProvider, cancellationToken);
            }
        }
    }

    ContainerChangeStream Join(IMongoDatabase database, string containerName, ChannelWriter<bool> subscriber)
    {
        var key = new ContainerKey(database.Client, database.DatabaseNamespace.DatabaseName, containerName);
        lock (_lock)
        {
            // A stream that has given up stays registered until its observers have left, but is not joined.
            if (!_streams.TryGetValue(key, out var stream) || stream.HasFailed)
            {
                stream = new ContainerChangeStream(key, database, logger, _timeProvider);
                _streams[key] = stream;
                _ = stream.Start();
            }

            stream.Add(subscriber);
            return stream;
        }
    }

    void Leave(ContainerChangeStream stream, ChannelWriter<bool> subscriber)
    {
        lock (_lock)
        {
            if (stream.Remove(subscriber) > 0)
            {
                return;
            }

            if (_streams.TryGetValue(stream.Key, out var current) && current == stream)
            {
                _streams.Remove(stream.Key);
            }

            stream.Dispose();
        }
    }

    /// <summary>
    /// Identifies an observed collection: the client and database it lives in, and its name.
    /// </summary>
    /// <param name="Client">The <see cref="IMongoClient"/> the collection is reached through.</param>
    /// <param name="DatabaseName">The name of the database.</param>
    /// <param name="ContainerName">The name of the collection.</param>
    internal sealed record ContainerKey(IMongoClient Client, string DatabaseName, string ContainerName);

    /// <summary>
    /// The one change stream shared by every observer of a collection.
    /// </summary>
    /// <param name="key">The <see cref="ContainerKey"/> of the observed collection.</param>
    /// <param name="database">The <see cref="IMongoDatabase"/> to watch.</param>
    /// <param name="logger">The <see cref="ILogger"/> for logging.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> the retry backoff waits on.</param>
    internal sealed class ContainerChangeStream(
        ContainerKey key,
        IMongoDatabase database,
        ILogger<ReadModelChangeStreams> logger,
        TimeProvider timeProvider) : IDisposable
    {
        readonly Lock _lock = new();
        readonly HashSet<ChannelWriter<bool>> _subscribers = [];

        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Released by Watch once it has returned, so it is never touched after it is gone")]
        readonly CancellationTokenSource _stopping = new();
        bool _isOpen;
        bool _isStopped;
        Exception? _failure;

        /// <summary>
        /// Gets the <see cref="ContainerKey"/> of the observed collection.
        /// </summary>
        public ContainerKey Key => key;

        /// <summary>
        /// Gets whether the stream has given up.
        /// </summary>
        public bool HasFailed
        {
            get
            {
                lock (_lock)
                {
                    return _failure is not null;
                }
            }
        }

        /// <summary>
        /// Start watching in the background.
        /// </summary>
        /// <returns>A <see cref="Task"/> completing when the watch has ended.</returns>
        public Task Start()
        {
            // The token is read here, not when the pool runs the delegate: a stream disposed straight away must not
            // have its source read after Watch has already ended and released it.
            var cancellationToken = _stopping.Token;
            return Task.Run(() => Watch(cancellationToken));
        }

        /// <summary>
        /// Add a subscriber to signal when the collection changes; a stream that has given up completes it with the failure.
        /// </summary>
        /// <param name="subscriber">The <see cref="ChannelWriter{T}"/> to signal.</param>
        public void Add(ChannelWriter<bool> subscriber)
        {
            lock (_lock)
            {
                if (_failure is not null)
                {
                    subscriber.TryComplete(_failure);
                    return;
                }

                _subscribers.Add(subscriber);

                // Joining an open stream: every change from here on is signalled, so the first read can go ahead.
                // Joining one that is not open yet: the first read waits for the signal it gives on opening.
                if (_isOpen)
                {
                    subscriber.TryWrite(true);
                }
            }
        }

        /// <summary>
        /// Remove a subscriber.
        /// </summary>
        /// <param name="subscriber">The <see cref="ChannelWriter{T}"/> to remove.</param>
        /// <returns>The number of subscribers left.</returns>
        public int Remove(ChannelWriter<bool> subscriber)
        {
            lock (_lock)
            {
                _subscribers.Remove(subscriber);
                return _subscribers.Count;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_isStopped)
                {
                    return;
                }

                // Only cancels: the source is released by Watch once it has returned, so nothing touches it disposed.
                _isStopped = true;
                _stopping.Cancel();
            }
        }

        async Task Watch(CancellationToken cancellationToken)
        {
            try
            {
                await WatchUntilStopped(cancellationToken);
            }
            finally
            {
                // Watch has returned, so it is safe to release the source; marking the stream stopped under the same
                // lock keeps a later Dispose from cancelling a source that is gone.
                lock (_lock)
                {
                    _isStopped = true;
                    _stopping.Dispose();
                }
            }
        }

        async Task WatchUntilStopped(CancellationToken cancellationToken)
        {
            var failures = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // The stream is open before anyone reads, so a write landing between opening and reading is not
                    // lost. Opening signals every observer, which also re-reads after a reopen anything that happened
                    // while no stream was open.
                    using var cursor = await database.WatchAsync(ChangesTo(key.ContainerName), cancellationToken: cancellationToken);
                    Opened();

                    while (await cursor.MoveNextAsync(cancellationToken))
                    {
                        failures = 0;
                        if (cursor.Current.Any())
                        {
                            Signal();
                        }
                    }

                    // The server ends a stream only by invalidating it; the loop opens a new one.
                    Closed();
                }
                catch (Exception) when (cancellationToken.IsCancellationRequested)
                {
                    // Anything that surfaces while the stream is being stopped is part of stopping it.
                    return;
                }
                catch (Exception exception) when (IsTransient(exception) && ++failures < MaxConsecutiveFailures)
                {
                    Closed();
                    logger.ChangeStreamFailed(key.ContainerName, failures, exception);
                    try
                    {
                        await Task.Delay(DelayAfter(failures), timeProvider, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
                catch (Exception exception)
                {
                    logger.ChangeStreamGaveUp(key.ContainerName, exception);
                    Fail(exception);
                    return;
                }
            }
        }

        void Opened()
        {
            lock (_lock)
            {
                _isOpen = true;
                SignalLocked();
            }
        }

        void Closed()
        {
            lock (_lock)
            {
                _isOpen = false;
            }
        }

        void Signal()
        {
            lock (_lock)
            {
                SignalLocked();
            }
        }

        void SignalLocked()
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.TryWrite(true);
            }
        }

        void Fail(Exception exception)
        {
            lock (_lock)
            {
                _isOpen = false;
                _failure = exception;
                foreach (var subscriber in _subscribers)
                {
                    subscriber.TryComplete(exception);
                }
            }
        }
    }
}
