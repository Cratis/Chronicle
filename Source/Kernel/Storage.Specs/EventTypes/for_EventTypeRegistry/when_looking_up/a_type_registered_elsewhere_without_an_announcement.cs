// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_looking_up;

public class a_type_registered_elsewhere_without_an_announcement : given.a_registry
{
    EventTypeSchema _found;
    int _readsForTwoMisses;

    async Task Establish() => await _registry.GetLatestForAllEventTypes();

    async Task Because()
    {
        _time.Advance(EventTypeRegistry.MissReloadInterval + TimeSpan.FromMilliseconds(1));
        _storage.Seed("late-event", (1, "late"));
        _found = await _registry.GetFor("late-event", 1);

        // Looking up something that does not exist reloads at most once per interval.
        var before = _storage.Reads;
        await Catch.Exception(() => _registry.GetFor("still-unknown", 1));
        await Catch.Exception(() => _registry.GetFor("still-unknown-too", 1));
        _readsForTwoMisses = _storage.Reads - before;
    }

    [Fact] void should_find_it() => _found.Schema.Description.ShouldEqual("late");
    [Fact] void should_not_reload_on_every_miss() => _readsForTwoMisses.ShouldBeLessThan(6);
}
