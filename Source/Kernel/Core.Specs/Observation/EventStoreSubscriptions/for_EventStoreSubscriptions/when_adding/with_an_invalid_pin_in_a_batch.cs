// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_an_invalid_pin_in_a_batch : given.a_pinned_registration
{
    void Establish()
    {
        Register("target", 1);
        _request.Subscriptions = [new() { Identifier = "compatibility", SourceEventStore = "source", EventTypes = [] }, _request.Subscriptions.Single()];
    }
    async Task Because() => _error = await Catch.Exception(() => _service.Add(_request));
    [Fact] void should_reject_the_whole_request_before_any_registration() => _error.ShouldBeOfExactType<EventTypeGenerationNotRegisteredForObserver>();
    [Fact] void should_not_reserve_even_the_first_valid_definition() => _manager.DidNotReceive().Add(Arg.Any<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition>());
    [Fact] void should_not_append_registration_events() => RegistrationEvents().ShouldEqual(0);
}
