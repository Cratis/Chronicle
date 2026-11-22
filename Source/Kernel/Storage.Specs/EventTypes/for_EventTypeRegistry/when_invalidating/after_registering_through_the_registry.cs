// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_invalidating;

public class after_registering_through_the_registry : given.a_registry
{
    bool _changed;
    bool _unchanged;
    EventTypeSchema _schema;
    int _readsBeforeUnchanged;
    int _readsAfterUnchanged;

    async Task Because()
    {
        await _registry.GetLatestForAllEventTypes();
        _changed = await _registry.Register(new(_eventTypeId, 3), new JsonSchema { Description = "third" });
        _schema = await _registry.GetFor(_eventTypeId, 3);

        _readsBeforeUnchanged = _storage.Reads;
        _unchanged = await _registry.Register(new(_eventTypeId, 3), new JsonSchema { Description = "third" });
        await _registry.GetFor(_eventTypeId, 3);
        _readsAfterUnchanged = _storage.Reads;
    }

    [Fact] void should_report_the_change() => _changed.ShouldBeTrue();
    [Fact] void should_resolve_the_new_generation_at_once_on_the_registering_silo() => _schema.Schema.Description.ShouldEqual("third");
    [Fact] void should_not_report_an_unchanged_registration() => _unchanged.ShouldBeFalse();
    [Fact] void should_not_reload_for_an_unchanged_registration() => _readsAfterUnchanged.ShouldEqual(_readsBeforeUnchanged);
}
