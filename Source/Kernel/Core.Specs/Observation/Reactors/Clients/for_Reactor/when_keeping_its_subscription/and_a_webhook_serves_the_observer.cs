// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_a_webhook_serves_the_observer : given.a_reactor_waiting_for_a_webhook
{
    Task Because() => _grain.KeepSubscription(_connectionDefinition);

    [Fact] async Task should_not_ask_the_observer_whether_it_knows_the_client() => await _observer.DidNotReceive().KnowsClient(Arg.Any<ConnectionId>());
    [Fact] Task should_not_restore_the_subscription() => WasRestored(0);
    [Fact] async Task should_not_unsubscribe_the_webhook() => await _observer.DidNotReceive().Unsubscribe();
}
