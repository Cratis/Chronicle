// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_last_write_under_refusal_finishes : given.a_replay
{
    int _clears;

    async Task Establish()
    {
        await _claims.Save("first", _definition, 0, Hash("a"));
        await _claims.Save("second", _definition, 1, Hash("b"));
        Event(0, "first", "Claimed", "b");
        Event(1, "second", "Claimed", "a");
        _replay = new(_sequence, _unique, _closed, _content, Microsoft.Extensions.Logging.Abstractions.NullLogger<ConstraintIndexReplay>.Instance, [new(_definition, EventSequenceNumber.First)], 2)
        {
            CatchUp = new(_ => false, _ => false, (_, _) => Task.CompletedTask, _ =>
            {
                _clears++;
                return Task.CompletedTask;
            })
        };
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_finish_the_swap() => _result.IsCompleted.ShouldBeTrue();
    [Fact] void should_release_the_durable_barrier_at_the_last_write() => _clears.ShouldEqual(1);
    [Fact] void should_stop_writing_under_refusal() => _replay.CatchUp!.UnderRefusal.ShouldBeFalse();
}
