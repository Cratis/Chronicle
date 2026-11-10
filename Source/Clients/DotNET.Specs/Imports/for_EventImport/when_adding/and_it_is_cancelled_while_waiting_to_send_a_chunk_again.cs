// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Imports;
using Grpc.Core;

namespace Cratis.Chronicle.Imports.for_EventImport.when_adding;

public class and_it_is_cancelled_while_waiting_to_send_a_chunk_again : given.an_event_import
{
    readonly CancellationTokenSource _cancellation = new();
    int _attempts;
    Exception? _error;

    void Establish()
    {
        _maxChunkEvents = 1;
        _retryDelay = _ => TimeSpan.FromHours(1);
        _service.AddImportChunk(Arg.Any<AddImportChunkRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(async call =>
        {
            var request = call.Arg<AddImportChunkRequest>();
            if (request.ChunkIndex != 0) return new CommandResult<AddImportChunkResponse> { Response = new() { ChunkIndex = request.ChunkIndex } };
            Interlocked.Increment(ref _attempts);
            await _cancellation.CancelAsync();
            throw new RpcException(new Status(StatusCode.Unavailable, "dropped"));
        });
    }

    async Task Because()
    {
        await Import.Add(Events(2), _cancellation.Token);
        _error = await Catch.Exception(() => Import.Complete());
    }

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_stop_waiting_instead_of_sleeping_out_the_delay() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_send_the_cancelled_chunk_again() => _attempts.ShouldEqual(1);
}
