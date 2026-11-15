// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Imports;
using Grpc.Core;

namespace Cratis.Chronicle.Imports.for_EventImport.when_adding;

public class and_the_connection_drops_while_sending_a_chunk : given.an_event_import
{
    readonly List<int> _sent = [];

    void Establish() => _service.AddImportChunk(Arg.Any<AddImportChunkRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(call =>
    {
        var request = call.Arg<AddImportChunkRequest>();
        lock (_sent)
        {
            _sent.Add(request.ChunkIndex);
            if (_sent.Count(_ => _ == request.ChunkIndex) == 1 && request.ChunkIndex == 1) throw new RpcException(new Status(StatusCode.Unavailable, "dropped"));
        }

        _chunks.Add(request);
        return Task.FromResult(new CommandResult<AddImportChunkResponse> { Response = new() { ChunkIndex = request.ChunkIndex } });
    });

    async Task Because()
    {
        await Import.Add(Events(6));
        await Import.Complete();
    }

    [Fact] void should_send_the_chunk_again_under_the_same_index() => _sent.Count(_ => _ == 1).ShouldEqual(2);
    [Fact] void should_have_every_chunk_acknowledged() => _chunks.Select(_ => _.ChunkIndex).Order().ShouldEqual([0, 1]);
}
