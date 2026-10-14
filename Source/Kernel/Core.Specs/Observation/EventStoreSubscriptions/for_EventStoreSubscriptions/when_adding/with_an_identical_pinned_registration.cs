// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_an_identical_pinned_registration : given.a_pinned_registration
{
    void Establish() => _existing = Existing(EventGenerationDelivery.Pinned, 3);
    async Task Because() => await _service.Add(_request);
    [Fact] void should_not_record_another_registration() => RegistrationEvents().ShouldEqual(0);
    [Fact] void should_not_replace_the_stored_pins() => _manager.DidNotReceive().Add(Arg.Any<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition>());
    [Fact] void should_reactivate_with_the_existing_definition() => _manager.Received(1).SourceEventStoreAdded(new EventStoreName("source"));
}
