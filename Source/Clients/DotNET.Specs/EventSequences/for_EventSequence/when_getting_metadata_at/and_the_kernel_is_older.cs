// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_metadata_at;

public class and_the_kernel_is_older : given.an_event_sequence
{
    Exception _error;

    void Establish() => _sequences.MetadataAt(Arg.Any<Contracts.Sequences.MetadataAtRequest>(), Arg.Any<CallContext>())
        .Returns(Task.FromException<QueryResult<IEnumerable<Contracts.Sequences.EventMetadataResponse>>>(new RpcException(new Status(StatusCode.Unimplemented, "old kernel"))));

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.GetMetadataAt(42UL));

    [Fact] void should_report_metadata_reads_as_unsupported() => _error.ShouldBeOfExactType<EventMetadataReadsNotSupported>();
    [Fact] void should_not_deserialize_content() => _eventSerializer.ReceivedCalls().ShouldBeEmpty();
}
