// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_array_items_change_during_an_addition : given.additive_evolution_enabled
{
    Exception _error;

    void Establish() => _definition = StoredEventType("some-event", (1, """{"type":"object","properties":{"names":{"type":"array","items":{"type":"string"}}}}"""));

    async Task Because() => _error = await Catch.Exception(() => Register(
        """{"type":"object","properties":{"names":{"type":"array","items":{"type":"integer"}},"age":{"type":"integer"}}}"""));

    [Fact] void should_reject_the_breaking_change() => _error.ShouldBeOfExactType<EventTypeSchemaChanged>();
}
