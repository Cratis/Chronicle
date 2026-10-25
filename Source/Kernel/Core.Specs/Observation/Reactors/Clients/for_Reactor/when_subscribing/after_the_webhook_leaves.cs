// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class after_the_webhook_leaves : given.a_reactor_waiting_for_a_webhook
{
    async Task Establish()
    {
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition);
        _observer.ClearReceivedCalls();
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
    }

    Task Because() => _grain.KeepSubscription(_connectionDefinition);

    [Fact] Task should_restore_the_client_subscription() => WasRestored(1);
}
