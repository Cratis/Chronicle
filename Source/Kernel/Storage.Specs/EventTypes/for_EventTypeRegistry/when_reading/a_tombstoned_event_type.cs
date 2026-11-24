// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_reading;

public class a_tombstoned_event_type : given.a_registry
{
    EventTypeSchema _schema;
    EventTypeSchema _fromList;

    void Establish() => _storage.SeedTombstone("gone-event", 1, "gone");

    async Task Because()
    {
        _schema = await _registry.GetFor("gone-event", 1);
        _fromList = (await _registry.GetLatestForAllEventTypes()).Single(_ => _.Type.Id == "gone-event");
    }

    [Fact] void should_still_resolve_it_for_replay() => _schema.Schema.Description.ShouldEqual("gone");
    [Fact] void should_flag_the_tombstone() => _schema.Type.Tombstone.ShouldBeTrue();
    [Fact] void should_flag_the_tombstone_in_the_list() => _fromList.Type.Tombstone.ShouldBeTrue();
}
