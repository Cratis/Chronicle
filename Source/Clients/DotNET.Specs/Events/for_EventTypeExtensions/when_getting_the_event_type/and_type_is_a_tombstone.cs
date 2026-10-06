// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_the_event_type;

public class and_type_is_a_tombstone : Specification
{
    [EventType]
    [Tombstone]
    record EntityRemoved;

    EventType _result;

    void Because() => _result = typeof(EntityRemoved).GetEventType();

    [Fact] void should_not_carry_the_tombstone_marker() => _result.Tombstone.ShouldBeFalse();
}
