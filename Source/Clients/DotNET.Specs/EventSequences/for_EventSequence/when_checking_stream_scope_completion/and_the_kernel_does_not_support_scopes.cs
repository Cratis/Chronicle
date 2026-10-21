// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using NSubstitute.ExceptionExtensions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_checking_stream_scope_completion;

public class and_the_kernel_does_not_support_scopes : given.an_event_sequence
{
    Exception _error;

    void Establish() => _sequences.IsStreamScopeCompleted(Arg.Any<Contracts.Sequences.IsStreamScopeCompletedRequest>(), CallContext.Default)
        .ThrowsAsync(new RpcException(new Status(StatusCode.Unimplemented, "Older kernel")));

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.IsStreamCompleted(new ClosedStreamScope(EventSourceId: "source")));

    [Fact] void should_report_the_unsupported_feature() => _error.ShouldBeOfExactType<ClosedStreamScopesNotSupported>();
}
