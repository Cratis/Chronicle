// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_tagged_routed_rpc_is_unimplemented : given.a_named_tag_append
{
    Exception _error;

    void Establish() => _sequences.AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), CallContext.Default)
        .Returns<Task<CommandResult<Contracts.Sequences.AppendManyResponse>>>(_ => throw new RpcException(new Status(StatusCode.Unimplemented, "No tagged RPC")));

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.AppendMany([new EventForEventSourceId(_sourceId, "one") { NamedTags = [new("name", "value")] }]));

    [Fact] void should_propagate_rpc_failure() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_not_retry_using_plain_rpc() => _sequences.DidNotReceive().AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default);
}
