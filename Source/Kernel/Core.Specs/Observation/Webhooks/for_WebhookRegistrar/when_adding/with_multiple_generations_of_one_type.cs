// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_multiple_generations_of_one_type : given.a_pinned_registration
{
    void Establish() => _definition.EventTypes.Add(new() { Id = "customer", Generation = 2 });
    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));
    [Fact] void should_reject_the_conflicting_pins() => _error.ShouldBeOfExactType<ObserverPinsMultipleGenerationsOfEventType>();
    [Fact] void should_leave_registration_unchanged() => _appended.ShouldBeEmpty();
}
