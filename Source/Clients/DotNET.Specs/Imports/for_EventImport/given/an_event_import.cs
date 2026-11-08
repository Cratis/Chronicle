// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Imports;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Imports.for_EventImport.given;

public class an_event_import : Specification
{
    protected IImports _service;
    protected ConcurrentBag<AddImportChunkRequest> _chunks;
    protected CompleteImportRequest? _completed;
    protected ImportOptions _options = ImportOptions.Default;
    protected int _maxChunkEvents = 3;
    protected int _maxChunkBytes = 1000;
    protected int _acknowledgedChunks;
    protected long _acknowledgedEvents;
    protected int _inFlight;
    protected int _maxInFlight;
    protected TaskCompletionSource? _gate;
    protected ImportId _importId = ImportId.New();
    protected ImportState _finalState = ImportState.Completed;
    protected Func<EventForEventSourceId, ImportEvent> _convert = @event => new ImportEvent
    {
        EventSourceId = @event.EventSourceId,
        Content = (string)@event.Event,
        EventType = new() { Id = "event", Generation = 1 }
    };

    protected Func<int, TimeSpan> _retryDelay = _ => TimeSpan.Zero;
    protected int _signalAtInFlight = int.MaxValue;
    protected TaskCompletionSource _inFlightReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EventImport? _import;

    protected EventImport Import => _import ??= new(
        _service,
        "store",
        "namespace",
        _importId,
        _options,
        _maxChunkEvents,
        _maxChunkBytes,
        @event => Task.FromResult(_convert(@event)),
        _acknowledgedChunks,
        _acknowledgedEvents,
        _retryDelay,
        TimeSpan.Zero);

    void Establish()
    {
        _chunks = [];
        _service = Substitute.For<IImports>();
        _service.AddImportChunk(Arg.Any<AddImportChunkRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(async call =>
        {
            var request = call.Arg<AddImportChunkRequest>();
            var current = Interlocked.Increment(ref _inFlight);
            InterlockedMax(ref _maxInFlight, current);
            if (current >= _signalAtInFlight) _inFlightReached.TrySetResult();
            if (_gate is not null) await _gate.Task;
            Interlocked.Decrement(ref _inFlight);
            _chunks.Add(request);
            return new CommandResult<AddImportChunkResponse>
            {
                Response = new() { ChunkIndex = request.ChunkIndex, EventsReceived = request.Events.Count(), EventsStaged = request.Events.Count() }
            };
        });
        _service.CompleteImport(Arg.Any<CompleteImportRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(call =>
        {
            _completed = call.Arg<CompleteImportRequest>();
            return new CommandResult<AbandonImportResponse> { Response = new() { ImportId = _importId.Value, State = Contracts.Imports.ImportState.Validating } };
        });
        _service.GetImport(Arg.Any<GetImportRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(_ => new QueryResult<ImportProgressResponse?>
        {
            Data = new() { ImportId = _importId.Value, State = (Contracts.Imports.ImportState)_finalState, EventsCommitted = _completed?.ExpectedEvents ?? 0 }
        });
    }

    protected static IEnumerable<EventForEventSourceId> Events(int count, int contentLength = 10) =>
        Enumerable.Range(0, count).Select(index => new EventForEventSourceId((EventSourceId)$"source-{index}", new string('x', contentLength)));

    static void InterlockedMax(ref int target, int value)
    {
        int current;
        while (value > (current = Volatile.Read(ref target)) && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }
}
