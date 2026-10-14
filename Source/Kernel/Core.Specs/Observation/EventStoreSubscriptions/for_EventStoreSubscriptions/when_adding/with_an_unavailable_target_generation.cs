// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_an_unavailable_target_generation : given.a_pinned_registration
{
    void Establish() => Register("target", 1);
    async Task Because() => _error = await Catch.Exception(() => _service.Add(_request));
    [Fact] void should_fail_explicitly() => _error.ShouldBeOfExactType<EventTypeGenerationNotRegisteredForObserver>();
    [Fact] void should_not_reserve_any_definition() => _manager.DidNotReceive().Add(Arg.Any<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition>());
    [Fact] void should_not_append_any_registration() => RegistrationEvents().ShouldEqual(0);
    [Fact] void should_not_depend_on_the_readiness_timeout() => _manager.DidNotReceive().WaitUntilSubscribed(Arg.Any<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionId>(), Arg.Any<TimeSpan>());
}
