// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Imports;
using Cratis.Chronicle.EventSequences;
using Grpc.Core;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents an implementation of <see cref="IEventImport"/> that cuts events into chunks and keeps a bounded number of
/// them in flight.
/// </summary>
/// <param name="service">The <see cref="IImports"/> contract.</param>
/// <param name="eventStore">The event store.</param>
/// <param name="namespace">The namespace.</param>
/// <param name="importId">The <see cref="Imports.ImportId"/>.</param>
/// <param name="options">The <see cref="ImportOptions"/>.</param>
/// <param name="maxChunkEvents">The most events the kernel accepts in one chunk.</param>
/// <param name="maxChunkBytes">The most bytes of event content the kernel accepts in one chunk.</param>
/// <param name="convert">Converts an event to its wire form.</param>
/// <param name="acknowledgedChunks">How many chunks the kernel already holds without a gap, when reattaching.</param>
/// <param name="acknowledgedEvents">How many events those chunks carry, when reattaching.</param>
/// <param name="retryDelay">Optional delay before an attempt to send a chunk again.</param>
/// <param name="pollInterval">Optional interval between reads of the import while waiting for its commit.</param>
public sealed class EventImport(
    IImports service,
    string eventStore,
    string @namespace,
    ImportId importId,
    ImportOptions options,
    int maxChunkEvents,
    int maxChunkBytes,
    Func<EventForEventSourceId, Task<ImportEvent>> convert,
    int acknowledgedChunks = 0,
    long acknowledgedEvents = 0,
    Func<int, TimeSpan>? retryDelay = null,
    TimeSpan? pollInterval = null) : IEventImport
{
    static readonly StatusCode[] _transientStatusCodes = [StatusCode.Unavailable, StatusCode.DeadlineExceeded, StatusCode.ResourceExhausted, StatusCode.Aborted, StatusCode.Internal];

    readonly IImports _service = service;
    readonly string _eventStore = eventStore;
    readonly string _namespace = @namespace;
    readonly ImportOptions _options = options;
    readonly Func<EventForEventSourceId, Task<ImportEvent>> _convert = convert;
    readonly Func<int, TimeSpan> _retryDelay = retryDelay ?? (attempt => TimeSpan.FromMilliseconds(Math.Min(5000, 200 * attempt * attempt)));
    readonly TimeSpan _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(500);
    readonly int _maxEventsPerChunk = Math.Max(1, Math.Min(maxChunkEvents <= 0 ? int.MaxValue : maxChunkEvents, options.MaxEventsPerChunk ?? int.MaxValue));
    readonly int _maxBytesPerChunk = maxChunkBytes <= 0 ? int.MaxValue : maxChunkBytes;
    readonly SemaphoreSlim _addLock = new(1, 1);
    readonly SemaphoreSlim _inFlight = new(Math.Max(1, options.MaxChunksInFlight));
    readonly List<Task> _sends = [];
    readonly List<ImportEvent> _buffer = [];
    readonly Lock _lock = new();
    int _bufferBytes;
    int _nextChunkIndex = acknowledgedChunks;
    long _nextEventIndex = acknowledgedEvents;
    Exception? _failure;
    bool _completed;

    /// <inheritdoc/>
    public ImportId ImportId { get; } = importId;

    /// <inheritdoc/>
    public long EventsAcknowledged { get; } = acknowledgedEvents;

    /// <inheritdoc/>
    public IObservable<ImportProgress> Progress => _service
        .ObserveImport(new() { EventStore = _eventStore, Namespace = _namespace, ImportId = ImportId.Value })
        .Where(_ => _.Data is not null)
        .Select(_ => _.Data.ToProgress());

    /// <inheritdoc/>
    public async Task Add(IEnumerable<EventForEventSourceId> events, CancellationToken cancellationToken = default)
    {
        await _addLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var @event in events)
            {
                await Buffer(@event, cancellationToken);
            }
        }
        finally
        {
            _addLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task Add(IAsyncEnumerable<EventForEventSourceId> events, CancellationToken cancellationToken = default)
    {
        await _addLock.WaitAsync(cancellationToken);
        try
        {
            await foreach (var @event in events.WithCancellation(cancellationToken))
            {
                await Buffer(@event, cancellationToken);
            }
        }
        finally
        {
            _addLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ImportResult> Complete(CancellationToken cancellationToken = default)
    {
        await _addLock.WaitAsync(cancellationToken);
        try
        {
            await Flush(cancellationToken);
            await Task.WhenAll(Sends());
            ThrowIfFailed();

            var status = (await _service.CompleteImport(new()
            {
                EventStore = _eventStore,
                Namespace = _namespace,
                ImportId = ImportId.Value,
                ExpectedChunks = _nextChunkIndex,
                ExpectedEvents = _nextEventIndex
            })).EnsureSuccess();
            _completed = true;

            if (status.Error is null && _options.WaitForCommit)
            {
                return await WaitForCommit(cancellationToken);
            }

            return await Read() ?? new ImportResult { ImportId = ImportId, State = (ImportState)status.State, FailureReason = status.Error };
        }
        finally
        {
            _addLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ImportResult> Abandon()
    {
        await Task.WhenAll(Sends()).ContinueWith(_ => { }, TaskScheduler.Default);
        var status = (await _service.AbandonImport(new() { EventStore = _eventStore, Namespace = _namespace, ImportId = ImportId.Value })).EnsureSuccess();
        _completed = true;
        return await Read() ?? new ImportResult { ImportId = ImportId, State = (ImportState)status.State, FailureReason = status.Error };
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // An import that was not completed stays open on the kernel so it can be reattached; only wait for what is in flight.
        if (!_completed)
        {
            await Task.WhenAll(Sends()).ContinueWith(_ => { }, TaskScheduler.Default);
        }

        _addLock.Dispose();
        _inFlight.Dispose();
    }

    static bool IsTerminal(ImportState state) => state is ImportState.Completed or ImportState.Failed or ImportState.Abandoned;

    async Task Buffer(EventForEventSourceId @event, CancellationToken cancellationToken)
    {
        ThrowIfFailed();
        var converted = await _convert(@event);
        var bytes = Encoding.UTF8.GetByteCount(converted.Content);
        if (_buffer.Count > 0 && (_buffer.Count >= _maxEventsPerChunk || _bufferBytes + bytes > _maxBytesPerChunk))
        {
            await Flush(cancellationToken);
        }

        _buffer.Add(converted);
        _bufferBytes += bytes;
    }

    async Task Flush(CancellationToken cancellationToken)
    {
        if (_buffer.Count == 0) return;

        var events = _buffer.ToArray();
        var chunkIndex = _nextChunkIndex++;
        var firstEventIndex = _nextEventIndex;
        _nextEventIndex += events.Length;
        _buffer.Clear();
        _bufferBytes = 0;

        await _inFlight.WaitAsync(cancellationToken);
        var send = Send(chunkIndex, firstEventIndex, events);
        lock (_lock)
        {
            _sends.RemoveAll(_ => _.IsCompleted);
            _sends.Add(send);
        }
    }

    async Task Send(int chunkIndex, long firstEventIndex, ImportEvent[] events)
    {
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    var result = (await _service.AddImportChunk(new()
                    {
                        EventStore = _eventStore,
                        Namespace = _namespace,
                        ImportId = ImportId.Value,
                        ChunkIndex = chunkIndex,
                        FirstEventIndex = firstEventIndex,
                        Events = events
                    })).EnsureSuccess();
                    if (result.Error is not null) throw new ImportChunkRefused(chunkIndex, result.Error);
                    return;
                }
                catch (RpcException ex) when (_transientStatusCodes.Contains(ex.StatusCode) && attempt < _options.MaxChunkAttempts)
                {
                    // Sending the chunk again is safe: the kernel stages a chunk under its index once.
                    await Task.Delay(_retryDelay(attempt));
                }
            }
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _failure ??= ex;
            }
        }
        finally
        {
            _inFlight.Release();
        }
    }

    Task[] Sends()
    {
        lock (_lock)
        {
            return [.. _sends];
        }
    }

    void ThrowIfFailed()
    {
        Exception? failure;
        lock (_lock)
        {
            failure = _failure;
        }

        if (failure is not null) throw failure;
    }

    async Task<ImportResult> WaitForCommit(CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await Read();
            if (result is not null && IsTerminal(result.State)) return result;
            await Task.Delay(_pollInterval, cancellationToken);
        }
    }

    async Task<ImportResult?> Read()
    {
        var progress = await _service.GetImport(new() { EventStore = _eventStore, Namespace = _namespace, ImportId = ImportId.Value });
        return progress.Data?.ToResult();
    }
}
