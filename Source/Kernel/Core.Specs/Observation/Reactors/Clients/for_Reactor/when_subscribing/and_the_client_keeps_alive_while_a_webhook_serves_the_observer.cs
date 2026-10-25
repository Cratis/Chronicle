// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class and_the_client_keeps_alive_while_a_webhook_serves_the_observer : given.a_reactor_waiting_for_a_webhook
{
    Task Because() => _grain.KeepSubscription(_connectionDefinition);

    [Fact] Task should_not_restore_the_client_subscription() => WasRestored(0);
    [Fact] async Task should_not_unsubscribe() => await _observer.DidNotReceive().Unsubscribe();
}
