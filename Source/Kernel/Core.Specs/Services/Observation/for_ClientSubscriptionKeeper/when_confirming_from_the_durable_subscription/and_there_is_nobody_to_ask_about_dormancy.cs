// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_confirming_from_the_durable_subscription;

public class and_there_is_nobody_to_ask_about_dormancy : Specification
{
    int _grainCalls;
    bool _intact;

    async Task Because() => _intact = await ClientSubscriptionKeeper.ConfirmFromDurableSubscription(
        () =>
        {
            _grainCalls++;
            return Task.FromResult(true);
        },
        null,
        new ConnectedObserverKey("an-observer", "an-event-store", "a-namespace", "a-sequence", new ConnectionId("a-connection")))();

    [Fact] void should_ask_the_client_grain_as_before() => _grainCalls.ShouldEqual(1);
    [Fact] void should_report_what_the_grain_answered() => _intact.ShouldBeTrue();
}
