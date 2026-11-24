// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_looking_up;

public class after_the_safety_net_lifetime : given.a_registry
{
    EventTypeSchema _within;
    EventTypeSchema _after;

    async Task Establish() => await _registry.GetFor(_eventTypeId, 2);

    async Task Because()
    {
        await _storage.Register(new(_eventTypeId, 2), given.PlainEventTypesStorage.SchemaFor("second changed"));

        _time.Advance(EventTypeRegistry.SafetyNetLifetime - TimeSpan.FromSeconds(1));
        _within = await _registry.GetFor(_eventTypeId, 2);

        _time.Advance(TimeSpan.FromSeconds(2));
        _after = await _registry.GetFor(_eventTypeId, 2);
    }

    [Fact] void should_still_serve_the_loaded_schema_within_the_lifetime() => _within.Schema.Description.ShouldEqual("second");
    [Fact] void should_reload_when_the_lifetime_has_passed() => _after.Schema.Description.ShouldEqual("second changed");
}
