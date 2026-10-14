// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding.and_an_event_is_redacted;

public class with_a_context_sourced_closing_scope : given.a_replay
{
    void Establish()
    {
        Event(0, "source", GlobalEventTypes.Redaction.Value);
        ((IDictionary<string, object?>)_events[0].Content)["originalEventType"] = "Closed";
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_apply_the_original_closing_event() => (await _closed.GetForOwner("closing")).Single().Scope.EventSourceId!.Value.ShouldEqual("source");
}
