// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.when_resolving;

public class and_it_is_registered_again_with_changes : given.an_event_type_with_two_generations
{
    EventTypeSchema _afterTombstone;
    IEnumerable<EventTypeSchema> _latest;
    int _publishedForUnchanged;
    int _publishedForChanged;

    async Task Because()
    {
        var published = 0;
        _storage.ObserveLatestForAllEventTypes().Subscribe(_ => published++);
        published = 0;

        await _storage.Register(second_generation, new JsonSchema { Description = "second" });
        _publishedForUnchanged = published;

        await _storage.Register(
            new EventType(second_generation.Id, second_generation.Generation, true),
            new JsonSchema { Description = "second" },
            visibility: EventTypeVisibility.Public,
            origin: "somewhere");
        _publishedForChanged = published - _publishedForUnchanged;

        _afterTombstone = await _storage.GetFor(second_generation.Id);
        _latest = await _storage.GetLatestForAllEventTypes();
    }

    [Fact] void should_not_publish_an_unchanged_registration() => _publishedForUnchanged.ShouldEqual(0);
    [Fact] void should_publish_the_change() => _publishedForChanged.ShouldEqual(1);
    [Fact] void should_flag_the_tombstone() => _afterTombstone.Type.Tombstone.ShouldBeTrue();
    [Fact] void should_carry_the_visibility() => _afterTombstone.Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_carry_the_origin() => _afterTombstone.Origin.ShouldEqual("somewhere");
    [Fact] void should_keep_the_latest_generation() => _latest.Single().Type.Generation.ShouldEqual(second_generation.Generation);
}
