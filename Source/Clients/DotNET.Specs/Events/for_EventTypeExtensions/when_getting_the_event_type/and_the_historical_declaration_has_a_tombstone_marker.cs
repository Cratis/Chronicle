// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_the_event_type;

public class and_the_historical_declaration_has_a_tombstone_marker : Specification
{
    [EventType(generation: 2)]
    record EntityRemoved;

    [EventTypeGenerationFor<EntityRemoved>(1)]
    [Tombstone]
    record EntityRemovedV1;

    EventType _result;

    void Because() => _result = typeof(EntityRemovedV1).GetEventType();

    [Fact] void should_preserve_the_declared_marker() => _result.Tombstone.ShouldBeTrue();
}
