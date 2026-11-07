// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences.when_registering_again_after_reconnect;

public class and_the_connection_dropped_again_first : given.a_registered_occurrence_client
{
    void Establish() => _service.RegisterTriggers(Arg.Any<RegisterTriggersRequest>(), Arg.Any<CallContext>()).Returns(async _ =>
    {
        await _lifecycle.Disconnected();
        return CommandResult<RegisterTriggersResponse>.Success(Guid.NewGuid(), new() { Registered = true });
    });

    async Task Because()
    {
        await _lifecycle.Disconnected();
        await _lifecycle.Connected();
    }

    [Fact] void should_not_start_a_worker_for_the_lost_connection() => _observations.ShouldEqual(1);
}
