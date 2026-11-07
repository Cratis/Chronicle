// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences.when_registering_again_after_reconnect;

public class and_automatic_registration_is_on : given.a_registered_occurrence_client
{
    protected override bool RegisterAgainAfterReconnect => false;

    async Task Because()
    {
        await _lifecycle.Disconnected();
        await _lifecycle.Connected();
    }

    [Fact] void should_leave_registration_to_the_automatic_path() => _service.Received(1).RegisterTriggers(Arg.Any<RegisterTriggersRequest>(), Arg.Any<CallContext>());
}
