// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Imports;

namespace Cratis.Chronicle.Imports.for_EventImport.when_adding;

public class and_a_chunk_is_refused : given.an_event_import
{
    Exception? _error;

    void Establish() => _service.AddImportChunk(Arg.Any<AddImportChunkRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
        .Returns(Task.FromResult(new CommandResult<AddImportChunkResponse> { Response = new() { Error = "import was abandoned" } }));

    async Task Because()
    {
        await Import.Add(Events(2));
        _error = await Catch.Exception(() => Import.Complete());
    }

    [Fact] void should_fail_completing() => _error.ShouldBeOfExactType<ImportChunkRefused>();
    [Fact] void should_not_complete_the_import() => _completed.ShouldBeNull();
}
