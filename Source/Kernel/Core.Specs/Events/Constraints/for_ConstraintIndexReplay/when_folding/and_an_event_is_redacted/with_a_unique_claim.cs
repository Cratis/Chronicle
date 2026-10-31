// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding.and_an_event_is_redacted;

public class with_a_unique_claim : given.a_replay
{
    void Establish()
    {
        Event(0, "source", "Claimed", "earlier");
        Event(1, "source", GlobalEventTypes.Redaction.Value);
        ((IDictionary<string, object?>)_events[1].Content)["originalEventType"] = "Claimed";
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_use_the_ranges_other_events_for_the_key() => (await Values("source")).ShouldContainOnly(Hash("earlier").Value);
}
