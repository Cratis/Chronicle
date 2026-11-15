// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Imports;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Imports.for_EventImport.when_abandoning;

/// <summary>
/// The producer's stream yields one event and then waits for something that ignores cancellation.
/// </summary>
public class and_an_add_reads_a_stream_that_does_not_yield : given.an_event_import
{
    readonly TaskCompletionSource _streamResumes = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _firstRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _adding;
    Exception? _abandonError;
    Exception? _addError;
    bool _abandoned;

    void Establish() => _service.AbandonImport(Arg.Any<AbandonImportRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(_ =>
    {
        _abandoned = true;
        return new CommandResult<AbandonImportResponse> { Response = new() { ImportId = _importId.Value, State = Contracts.Imports.ImportState.Abandoned } };
    });

    async Task Because()
    {
        _adding = Import.Add(Stream());
        await _firstRead.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _abandonError = await Catch.Exception(() => Import.Abandon().WaitAsync(TimeSpan.FromSeconds(5)));
        _streamResumes.SetResult();
        _addError = await Catch.Exception(() => _adding.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact] void should_abandon_without_waiting_for_the_stream() => _abandonError.ShouldBeNull();
    [Fact] void should_abandon_the_import() => _abandoned.ShouldBeTrue();
    [Fact] void should_end_the_add_once_the_stream_yields_again() => _addError.ShouldBeOfExactType<ImportWasAbandoned>();
    [Fact] void should_send_nothing() => _chunks.ShouldBeEmpty();

    async IAsyncEnumerable<EventForEventSourceId> Stream()
    {
        foreach (var @event in Events(2))
        {
            yield return @event;
            _firstRead.TrySetResult();
            await _streamResumes.Task;
        }
    }
}
