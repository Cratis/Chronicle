// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences.when_registering_again_after_reconnect;

public class and_the_occurrences_were_disposed : given.a_registered_occurrence_client
{
    async Task Because()
    {
        await _lifecycle.Disconnected();
        await _occurrences.DisposeAsync();
        await _lifecycle.Connected();
    }

    [Fact] void should_not_register_triggers_again() => _service.Received(1).RegisterTriggers(Arg.Any<RegisterTriggersRequest>(), Arg.Any<CallContext>());
}
