// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_a_substituted_target_schema : given.a_pinned_registration
{
    void Establish() => _storage.GetEventStore("target").EventTypes.GetFor(_pin.Id, _pin.Generation).Returns(new EventTypeSchema(new(_pin.Id, 1), EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromJson("{\"type\":\"object\"}")));
    async Task Because() => _error = await Catch.Exception(() => _service.Add(_request));
    [Fact] void should_require_the_exact_schema_as_well_as_the_definition() => _error.ShouldBeOfExactType<EventTypeGenerationNotRegisteredForObserver>();
    [Fact] void should_not_append_any_registration() => RegistrationEvents().ShouldEqual(0);
}
