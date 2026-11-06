// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences.when_registering_again_after_reconnect;

public class and_it_was_registered_before : given.a_registered_occurrence_client
{
    async Task Because()
    {
        await _lifecycle.Disconnected();
        await _lifecycle.Connected();
        await _observedAgain.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
    }

    [Fact] void should_register_the_triggers_again() => _service.Received(2).RegisterTriggers(Arg.Any<RegisterTriggersRequest>(), Arg.Any<CallContext>());
    [Fact] void should_start_a_new_observation() => _observations.ShouldEqual(2);
    [Fact] void should_observe_notifications() => _notifications.HasObservers.ShouldBeTrue();
}
