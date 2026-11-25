// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_confirming_from_the_observer;

public class and_the_observer_does_not_know_the_client : given.a_confirmation_from_the_observer
{
    void Establish()
    {
        ObserverKnowsTheClient(false);
        _grainAnswer = false;
    }

    Task Because() => Confirm();

    [Fact] void should_not_find_the_subscription_intact() => _intact.ShouldBeFalse();
    [Fact] void should_ask_the_client_grain_which_restores_what_was_lost() => _grainCalls.ShouldEqual(1);
}
