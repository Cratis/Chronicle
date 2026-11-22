// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ClientReducerSubscription.when_subscribing;

public class and_the_subscription_is_accepted : given.a_client_reducer_subscription_with_a_stored_definition
{
    Task Because() => _subscription.SetDefinitionAndSubscribe(_connectionDefinition);

    [Fact] Task should_record_the_observer_for_the_connection() => _connectedClients.Received(1).RecordObserver(_client.ConnectionId, new ObserverKey("the-reducer", "the-event-store", "the-namespace", EventSequenceId.Log));
}
