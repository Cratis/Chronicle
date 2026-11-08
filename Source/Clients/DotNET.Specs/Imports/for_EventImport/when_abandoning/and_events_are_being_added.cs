// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Imports;

namespace Cratis.Chronicle.Imports.for_EventImport.when_abandoning;

public class and_events_are_being_added : given.an_event_import
{
    Task _adding;
    Task _abandoning;
    bool _abandonedBeforeAddingEnded;

    void Establish()
    {
        _options = ImportOptions.Default with { MaxChunksInFlight = 1 };
        _maxChunkEvents = 1;
        _signalAtInFlight = 1;
        _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _service.AbandonImport(Arg.Any<AbandonImportRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(_ =>
        {
            _abandonedBeforeAddingEnded = !_adding.IsCompleted;
            return new CommandResult<AbandonImportResponse> { Response = new() { ImportId = _importId.Value, State = Contracts.Imports.ImportState.Abandoned } };
        });
    }

    async Task Because()
    {
        _adding = Import.Add(Events(3));
        await _inFlightReached.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _abandoning = Import.Abandon();
        _gate!.SetResult();
        await _adding;
        await _abandoning;
    }

    [Fact] void should_wait_for_the_adding_to_end_before_abandoning() => _abandonedBeforeAddingEnded.ShouldBeFalse();
}
