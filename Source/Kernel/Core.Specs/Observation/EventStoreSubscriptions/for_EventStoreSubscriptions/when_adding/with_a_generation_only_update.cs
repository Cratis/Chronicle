// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_a_generation_only_update : given.a_pinned_registration
{
    void Establish() => _existing = Existing(EventGenerationDelivery.Pinned, 2);
    async Task Because() => await _service.Add(_request);
    [Fact] void should_record_the_deliberate_new_pin() => AddedEvent().EventTypes.ShouldContainOnly(_pin);
    [Fact] void should_not_remove_the_subscription() => _manager.DidNotReceive().Remove(Arg.Any<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionId>());
}
