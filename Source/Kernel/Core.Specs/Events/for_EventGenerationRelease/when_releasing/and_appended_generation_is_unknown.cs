// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_appended_generation_is_unknown : given.a_release_boundary
{
    void Establish()
    {
        _definition = _definition with { Generations = [.. _definition.Generations, new(3, CreateSchema("newName"))] };
        _event = _event with
        {
            Context = _event.Context with { AppendedGeneration = null, EventType = new(_pin.Id, 3) },
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"old-name\"}", [3] = "{\"newName\":\"highest-name\"}" }
        };
    }

    async Task Because() => _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);

    [Fact] void should_migrate_from_the_highest_stored_generation() => ((IDictionary<string, object?>)_result[0].Content)["fullName"].ShouldEqual("highest-name");
    [Fact] void should_keep_appended_generation_unknown() => _result[0].Context.AppendedGeneration.ShouldBeNull();
}
