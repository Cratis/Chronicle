// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_unsubscribing;

public class and_the_client_disconnects : given.a_reactor_grain_with_a_stored_definition
{
    Task Because() => _grain.Unsubscribe();

    [Fact] async Task should_only_unsubscribe_if_the_observer_is_still_served_by_the_client() => await _observer.Received(1).UnsubscribeIfMatchesClient(_client.ConnectionId);
    [Fact] async Task should_not_unsubscribe_the_observer_unconditionally() => await _observer.DidNotReceive().Unsubscribe();
}
