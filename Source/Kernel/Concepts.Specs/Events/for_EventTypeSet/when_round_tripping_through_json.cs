// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeSet;

public class when_round_tripping_through_json : Specification
{
    EventTypeSet _original;
    EventTypeSet _roundTripped;

    void Because()
    {
        _original = EventTypeSet.From([new EventType("json-type", 1), new EventType("other-json-type", 3)]);
        _roundTripped = JsonSerializer.Deserialize<EventTypeSet>(JsonSerializer.Serialize(_original))!;
    }

    [Fact] void should_come_back_as_the_shared_set() => ReferenceEquals(_original, _roundTripped).ShouldBeTrue();
}
