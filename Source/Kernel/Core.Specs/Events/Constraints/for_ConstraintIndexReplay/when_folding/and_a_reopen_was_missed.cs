// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_reopen_was_missed : given.a_replay
{
    async Task Establish()
    {
        var scope = new ClosedStreamScope(EventSourceId: "source");
        await _closed.Close(new(scope, "closing", 0, DateTimeOffset.UtcNow));
        await _closed.Close(new(scope, ClosedStreamOwner.Manual, 0, DateTimeOffset.UtcNow));
        Event(1, "source", "Reopened");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_reopen_the_owned_scope() => (await _closed.GetForOwner("closing")).ShouldBeEmpty();
    [Fact] async Task should_preserve_the_manual_closure() => (await _closed.GetForOwner(ClosedStreamOwner.Manual)).Count().ShouldEqual(1);
}
