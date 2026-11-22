// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_invalidating;

public class while_a_load_is_in_flight : given.a_registry
{
    TaskCompletionSource _gate;
    EventTypeSchema _afterAnnouncement;

    async Task Because()
    {
        // The load reads storage before the registration lands and is held there; the announcement arrives while
        // it is still in flight, after which the storage holds the new schema.
        _gate = new();
        _storage.ReadGate = _gate;
        var loading = _registry.GetLatestForAllEventTypes();
        await _storage.Register(new(_eventTypeId, 2), given.PlainEventTypesStorage.SchemaFor("second changed"));
        _registry.Invalidate(_eventTypeId);
        _storage.ReadGate = null;
        _gate.SetResult();
        await loading;

        _afterAnnouncement = await _registry.GetFor(_eventTypeId, 2);
    }

    [Fact] void should_never_serve_the_schema_older_than_the_announced_registration() => _afterAnnouncement.Schema.Description.ShouldEqual("second changed");
}
