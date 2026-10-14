// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_a_mode_only_update : given.a_pinned_registration
{
    void Establish() => _existing = Existing(EventGenerationDelivery.Compatibility, 3);
    async Task Because() => await _service.Add(_request);
    [Fact] void should_record_the_deliberate_mode_update() => AddedEvent().GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_keep_the_same_exact_pin() => AddedEvent().EventTypes.ShouldContainOnly(_pin);
}
