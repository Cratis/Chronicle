// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_confirming_from_the_observer;

public class and_the_observer_knows_the_client : given.a_confirmation_from_the_observer
{
    void Establish() => ObserverKnowsTheClient(true);

    Task Because() => Confirm();

    [Fact] void should_find_the_subscription_intact() => _intact.ShouldBeTrue();
    [Fact] void should_not_call_the_client_grain_so_it_can_go_idle() => _grainCalls.ShouldEqual(0);
}
