// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_metadata_at;

public class and_the_kernel_is_unavailable : given.an_event_sequence
{
    RpcException _transportError;
    Exception _error;

    void Establish()
    {
        _transportError = new RpcException(new Status(StatusCode.Unavailable, "unavailable"));
        _sequences.MetadataAt(Arg.Any<Contracts.Sequences.MetadataAtRequest>(), Arg.Any<CallContext>())
            .Returns(Task.FromException<QueryResult<IEnumerable<Contracts.Sequences.EventMetadataResponse>>>(_transportError));
    }

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.GetMetadataAt(42UL));

    [Fact] void should_preserve_other_transport_failures() => _error.ShouldEqual(_transportError);
}
