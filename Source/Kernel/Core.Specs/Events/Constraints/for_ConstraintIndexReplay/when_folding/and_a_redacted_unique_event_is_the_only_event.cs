// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_redacted_unique_event_is_the_only_event : given.a_replay
{
    async Task Establish()
    {
        await _claims.Save("source", _definition, 0, Hash("live"));
        Event(0, "source", GlobalEventTypes.Redaction.Value);
        ((IDictionary<string, object?>)_events[0].Content)["originalEventType"] = "Claimed";
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_keep_the_current_claim() => (await Values("source")).ShouldContainOnly(Hash("live").Value);
    [Fact] void should_write_nothing() => _writes.ShouldBeEmpty();
}
