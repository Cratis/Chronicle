// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_confirming_from_the_observer;

public class and_asking_the_observer_fails : given.a_confirmation_from_the_observer
{
    void Establish() => _observer.KnowsClient(_connectionId).Returns<Task<bool>>(_ => throw new InvalidOperationException("The observer is not available."));

    Task Because() => Confirm();

    [Fact] void should_fall_back_to_the_client_grain() => _grainCalls.ShouldEqual(1);
    [Fact] void should_use_the_answer_of_the_client_grain() => _intact.ShouldBeTrue();
}
