// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistry;

public class when_registering_without_capabilities : given.an_occurrence_client
{
    void Establish()
    {
        _registry.Discover();
        ((IKernelCapabilities)_connection).Capabilities.Returns([]);
    }

    async Task Because() => _error = await Catch.Exception(_registry.Register);

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<OccurrencesNotSupported>();
    [Fact] void should_not_send_registration() => _service.DidNotReceive().RegisterTriggers(Arg.Any<RegisterTriggersRequest>(), Arg.Any<CallContext>());
}
