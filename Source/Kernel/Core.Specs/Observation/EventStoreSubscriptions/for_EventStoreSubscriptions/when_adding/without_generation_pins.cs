// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class without_generation_pins : given.a_pinned_registration
{
    void Establish() => _request.Subscriptions.Single().EventTypes.Clear();
    async Task Because() => _error = await Catch.Exception(() => _service.Add(_request));
    [Fact] void should_reject_a_pinned_wildcard_registration() => _error.ShouldBeOfExactType<EventStoreSubscriptionHasNoGenerationPins>();
    [Fact] void should_not_record_a_registration() => RegistrationEvents().ShouldEqual(0);
}
