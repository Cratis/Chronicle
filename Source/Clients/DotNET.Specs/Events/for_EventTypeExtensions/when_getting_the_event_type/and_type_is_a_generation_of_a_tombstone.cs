// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_the_event_type;

public class and_type_is_a_generation_of_a_tombstone : Specification
{
    [EventType(generation: 2)]
    [Tombstone]
    record EntityRemoved;

    [EventTypeGenerationFor<EntityRemoved>(1)]
    record EntityRemovedV1;

    EventType _result;

    void Because() => _result = typeof(EntityRemovedV1).GetEventType();

    [Fact] void should_preserve_the_tombstone_flag() => _result.Tombstone.ShouldBeTrue();
    [Fact] void should_preserve_the_generation() => _result.Generation.Value.ShouldEqual(1u);
}
