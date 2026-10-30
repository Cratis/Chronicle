// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.given;

public class a_reducer_waiting_for_a_webhook : a_reducer_grain_with_a_stored_definition
{
    void Establish() => _observer.GetSubscription().Returns(new ObserverSubscription(
        _connectionDefinition.Identifier,
        new ObserverKey(_connectionDefinition.Identifier, "the-event-store", "the-namespace", _connectionDefinition.EventSequenceId),
        [],
        typeof(IWebhookObserverSubscriber),
        SiloAddress.Zero));
}
