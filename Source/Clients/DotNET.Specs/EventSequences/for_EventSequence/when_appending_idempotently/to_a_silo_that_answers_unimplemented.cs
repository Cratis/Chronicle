// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_idempotently;

public class to_a_silo_that_answers_unimplemented : given.an_idempotent_append
{
    Exception _error;

    void Establish() => _sequences.AppendOnce(Arg.Any<Contracts.Sequences.AppendOnceRequest>(), CallContext.Default).Returns<Task<Contracts.Commands.CommandResult<Contracts.Sequences.AppendOnceResponse>>>(_ => throw new RpcException(new Status(StatusCode.Unimplemented, "no")));

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.AppendOnce("source", "event", _key));

    [Fact] void should_refuse_instead_of_appending_unkeyed() => _error.ShouldBeOfExactType<IdempotentAppendsNotSupported>();
}
