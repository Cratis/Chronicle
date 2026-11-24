// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_two_silos_share_storage;

public class and_one_registers_and_announces : given.a_registry
{
    EventTypeRegistry _siloB;
    EventTypeSchema _siloBBeforeAnnouncement;
    EventTypeSchema _siloBAfterAnnouncement;
    EventTypeSchema _siloAAfterRegistering;

    async Task Establish()
    {
        _siloB = new(_storage, _time);
        await _siloB.GetFor(_eventTypeId, 2);
    }

    async Task Because()
    {
        await _registry.Register(new(_eventTypeId, 2), new JsonSchema { Description = "second changed" });
        _siloAAfterRegistering = await _registry.GetFor(_eventTypeId, 2);
        _siloBBeforeAnnouncement = await _siloB.GetFor(_eventTypeId, 2);

        // What the grain service does on silo B once silo A has announced the change.
        _siloB.Invalidate(_eventTypeId);
        _siloBAfterAnnouncement = await _siloB.GetFor(_eventTypeId, 2);
    }

    [Fact] void should_resolve_the_change_on_the_registering_silo() => _siloAAfterRegistering.Schema.Description.ShouldEqual("second changed");
    [Fact] void should_not_have_reached_the_other_silo_before_the_announcement() => _siloBBeforeAnnouncement.Schema.Description.ShouldEqual("second");
    [Fact] void should_resolve_the_change_on_the_other_silo_once_announced() => _siloBAfterAnnouncement.Schema.Description.ShouldEqual("second changed");
}
