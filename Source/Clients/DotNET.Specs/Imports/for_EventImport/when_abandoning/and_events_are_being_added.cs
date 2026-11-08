// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Imports;

namespace Cratis.Chronicle.Imports.for_EventImport.when_abandoning;

/// <summary>
/// One chunk is in flight and the Add waits for room to send the next when the import is abandoned.
/// </summary>
public class and_events_are_being_added : given.an_event_import
{
    Task _adding;
    Exception? _addError;
    bool _abandonedBeforeTheChunkInFlightSettled;

    void Establish()
    {
        _options = ImportOptions.Default with { MaxChunksInFlight = 1 };
        _maxChunkEvents = 1;
        _signalAtInFlight = 1;
        _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _service.AbandonImport(Arg.Any<AbandonImportRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(_ =>
        {
            _abandonedBeforeTheChunkInFlightSettled = _chunks.IsEmpty;
            return new CommandResult<AbandonImportResponse> { Response = new() { ImportId = _importId.Value, State = Contracts.Imports.ImportState.Abandoned } };
        });
    }

    async Task Because()
    {
        _adding = Import.Add(Events(3));
        await _inFlightReached.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var abandoning = Import.Abandon();
        _addError = await Catch.Exception(() => _adding.WaitAsync(TimeSpan.FromSeconds(5)));
        _gate!.SetResult();
        await abandoning.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_stop_the_add_without_waiting_for_it() => _addError.ShouldBeOfExactType<ImportWasAbandoned>();
    [Fact] void should_send_no_chunk_after_abandoning() => _chunks.Select(_ => _.ChunkIndex).ShouldContainOnly(0);
    [Fact] void should_let_the_chunk_in_flight_settle_before_abandoning() => _abandonedBeforeTheChunkInFlightSettled.ShouldBeFalse();
    [Fact] async Task should_refuse_more_events() => (await Catch.Exception(() => Import.Add(Events(1)))).ShouldBeOfExactType<ImportWasAbandoned>();
    [Fact] async Task should_refuse_to_complete() => (await Catch.Exception(() => Import.Complete())).ShouldBeOfExactType<ImportWasAbandoned>();
}
