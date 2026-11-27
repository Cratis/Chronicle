// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_confirming_from_the_observer;

public class and_setup_is_not_established : given.a_confirmation_from_the_observer
{
    void Establish()
    {
        ObserverKnowsTheClient(true);
        _observer.IsSubscriptionEstablished().Returns(false);
        _grainAnswer = false;
    }

    Task Because() => Confirm();

    [Fact] void should_ask_the_client_grain() => _grainCalls.ShouldEqual(1);
    [Fact] void should_not_confirm_failed_setup() => _intact.ShouldBeFalse();
}
