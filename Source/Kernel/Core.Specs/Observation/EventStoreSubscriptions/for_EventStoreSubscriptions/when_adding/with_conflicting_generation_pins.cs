// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_conflicting_generation_pins : given.a_pinned_registration
{
    void Establish() => _request.Subscriptions.Single().EventTypes.Add(new() { Id = _pin.Id, Generation = 2 });
    async Task Because() => _error = await Catch.Exception(() => _service.Add(_request));
    [Fact] void should_reject_conflicting_pins() => _error.ShouldBeOfExactType<ObserverPinsMultipleGenerationsOfEventType>();
    [Fact] void should_not_append_any_registration() => RegistrationEvents().ShouldEqual(0);
}
