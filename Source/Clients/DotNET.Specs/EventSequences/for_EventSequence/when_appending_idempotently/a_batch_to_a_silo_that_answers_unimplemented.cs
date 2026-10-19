// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_idempotently;

public class a_batch_to_a_silo_that_answers_unimplemented : given.an_idempotent_append
{
    Exception _error;

    void Establish() => _sequences.AppendManyOnce(Arg.Any<Contracts.Sequences.AppendManyOnceRequest>(), CallContext.Default).Returns<Task<Contracts.Commands.CommandResult<Contracts.Sequences.AppendManyOnceResponse>>>(_ => throw new RpcException(new Status(StatusCode.Unimplemented, "no")));

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.AppendManyOnce("source", ["one"], _key));

    [Fact] void should_refuse_instead_of_appending_unkeyed() => _error.ShouldBeOfExactType<IdempotentAppendsNotSupported>();
    [Fact] void should_not_fall_back_to_an_unkeyed_batch() => _sequences.DidNotReceive().AppendMany(Arg.Any<Contracts.Sequences.AppendManyRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fall_back_to_an_unkeyed_batch_for_event_sources() => _sequences.DidNotReceive().AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>());
}
