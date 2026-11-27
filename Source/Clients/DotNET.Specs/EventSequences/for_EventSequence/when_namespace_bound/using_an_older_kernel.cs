// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Namespaces;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class using_an_older_kernel : given.a_namespace_bound_append
{
    Exception _error;

    void Establish() => _sequences.AppendToNamespace(Arg.Any<Contracts.Sequences.AppendToNamespaceRequest>(), CallContext.Default)
        .Returns(Task.FromException<CommandResult<Contracts.Sequences.AppendResponse>>(new RpcException(new(StatusCode.Unimplemented, "unsupported"))));

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.Append("source", "event"));

    [Fact] void should_report_unsupported() => _error.ShouldBeOfExactType<NamespaceBindingsNotSupported>();
    [Fact] void should_not_downgrade() => ShouldNotUseLegacyAppendRoutes();
}
