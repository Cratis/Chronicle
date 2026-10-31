// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_an_unregistered_pin_after_a_valid_definition : given.a_pinned_registration
{
    void Establish() => _eventTypes.HasFor("customer", 3).Returns(false);
    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [new Contracts.Observation.Webhooks.WebhookDefinition { Identifier = "valid" }, _definition]));
    [Fact] void should_reject_the_unregistered_pin() => _error.ShouldBeOfExactType<EventTypeGenerationNotRegisteredForObserver>();
    [Fact] void should_leave_every_registration_unchanged() => _appended.ShouldBeEmpty();
}
