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
#if NET8_0
    readonly object _lock = new();
#else
    readonly Lock _lock = new();
#endif
    readonly CancellationTokenSource _abandoning = new();
    int _bufferBytes;
    int _nextChunkIndex = acknowledgedChunks;
    long _nextEventIndex = acknowledgedEvents;
    Exception? _failure;
    bool _abandoned;
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
    public Task Add(IEnumerable<EventForEventSourceId> events, CancellationToken cancellationToken = default) =>
        WhileNotAbandoned(
            async waitToken =>
            {
                foreach (var @event in events)
                {
                    await Buffer(@event, waitToken, cancellationToken);
                }
            },
            cancellationToken);

    /// <inheritdoc/>
    public Task Add(IAsyncEnumerable<EventForEventSourceId> events, CancellationToken cancellationToken = default) =>
        WhileNotAbandoned(
            async waitToken =>
            {
                await foreach (var @event in events.WithCancellation(waitToken))
                {
                    await Buffer(@event, waitToken, cancellationToken);
                }
            },
            cancellationToken);

    /// <inheritdoc/>
    public async Task<ImportResult> Complete(CancellationToken cancellationToken = default)
    {
        ImportResult? result = null;
        await WhileNotAbandoned(
            async waitToken =>
            {
                await Flush(waitToken, cancellationToken);
                await Task.WhenAll(Sends());
                ThrowIfFailed();
                ThrowIfAbandoned();

                var status = (await _service.CompleteImport(new()
                {
                    EventStore = _eventStore,
                    Namespace = _namespace,
                    ImportId = ImportId.Value,
                    ExpectedChunks = _nextChunkIndex,
                    ExpectedEvents = _nextEventIndex
                })).EnsureSuccess();
                _completed = true;

                result = status.Error is null && _options.WaitForCommit
                    ? await WaitForCommit(cancellationToken)
                    : await Read() ?? new ImportResult { ImportId = ImportId, State = (ImportState)status.State, FailureReason = status.Error };
            },
            cancellationToken);
        return result!;
    }

    /// <inheritdoc/>
    public async Task<ImportResult> Abandon(CancellationToken cancellationToken = default)
    {
        // No chunk is sent once this is set, and an Add in progress is stopped rather than waited for: it may be reading
        // a stream that never yields again.
        lock (_lock)
        {
            _abandoned = true;
        }

        await _abandoning.CancelAsync();

        // Chunks already handed to the kernel settle first, so none is staged after the import is abandoned.
        await Task.WhenAll(Sends()).ContinueWith(_ => { }, TaskScheduler.Default).WaitAsync(cancellationToken);
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
        _abandoning.Dispose();
    }

    static bool IsTerminal(ImportState state) => state is ImportState.Completed or ImportState.Failed or ImportState.Abandoned;

    /// <summary>
    /// Measures an event as the kernel counts it against its chunk byte limit: the content, tags, named tags, causation
    /// and idempotency key.
    /// </summary>
    /// <param name="event">The <see cref="ImportEvent"/> in its wire form.</param>
    /// <returns>The bytes it carries, as UTF-8.</returns>
    static int SizeOf(ImportEvent @event)
    {
        var bytes = Utf8(@event.Content) + Utf8(@event.IdempotencyKey);
        foreach (var tag in @event.Tags ?? []) bytes += Utf8(tag);
        foreach (var tag in @event.NamedTags ?? []) bytes += Utf8(tag.Name) + Utf8(tag.Value);
        foreach (var causation in @event.Causation ?? [])
        {
            bytes += Utf8(causation.Type);
            foreach (var (name, value) in causation.Properties) bytes += Utf8(name) + Utf8(value);
        }

        return bytes;
    }

    static int Utf8(string? value) => value is null ? 0 : Encoding.UTF8.GetByteCount(value);

    async Task WhileNotAbandoned(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _abandoning.Token);
        try
        {
            ThrowIfAbandoned();
            await _addLock.WaitAsync(waiting.Token);
            try
            {
                await work(waiting.Token);
            }
            finally
            {
                _addLock.Release();
            }
        }
        catch (OperationCanceledException) when (_abandoning.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new ImportWasAbandoned(ImportId);
        }
    }

    async Task Buffer(EventForEventSourceId @event, CancellationToken waitToken, CancellationToken cancellationToken)
    {
        ThrowIfFailed();
        ThrowIfAbandoned();
        var converted = await _convert(@event);
        var bytes = SizeOf(converted);
        if (_buffer.Count > 0 && (_buffer.Count >= _maxEventsPerChunk || _bufferBytes + bytes > _maxBytesPerChunk))
        {
            await Flush(waitToken, cancellationToken);
        }

        _buffer.Add(converted);
        _bufferBytes += bytes;
    }

    async Task Flush(CancellationToken waitToken, CancellationToken cancellationToken)
    {
        if (_buffer.Count == 0) return;

        var events = _buffer.ToArray();
        var chunkIndex = _nextChunkIndex;
        var firstEventIndex = _nextEventIndex;

        await _inFlight.WaitAsync(waitToken);
        lock (_lock)
        {
            // Checked and registered together, so Abandon sees every chunk sent before it and none is sent after it.
            if (_abandoned)
            {
                _inFlight.Release();
                throw new ImportWasAbandoned(ImportId);
            }

            _nextChunkIndex++;
            _nextEventIndex += events.Length;
            _buffer.Clear();
            _bufferBytes = 0;
            _sends.RemoveAll(_ => _.IsCompleted);
            _sends.Add(Task.Run(() => Send(chunkIndex, firstEventIndex, events, cancellationToken), CancellationToken.None));
        }
    }

    async Task Send(int chunkIndex, long firstEventIndex, ImportEvent[] events, CancellationToken cancellationToken)
    {
        using var retrying = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _abandoning.Token);
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
                    await Task.Delay(_retryDelay(attempt), retrying.Token);
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

    void ThrowIfAbandoned()
    {
        lock (_lock)
        {
            if (_abandoned) throw new ImportWasAbandoned(ImportId);
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
