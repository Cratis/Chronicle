// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_confirming_from_the_observer;

public class and_asking_about_setup_fails : given.a_confirmation_from_the_observer
{
    void Establish()
    {
        ObserverKnowsTheClient(true);
        _observer.IsSubscriptionEstablished().Returns(Task.FromException<bool>(new InvalidOperationException("Setup unavailable")));
    }

    Task Because() => Confirm();

    [Fact] void should_fall_back_to_the_client_grain() => _grainCalls.ShouldEqual(1);
    [Fact] void should_use_the_client_grain_answer() => _intact.ShouldBeTrue();
}
