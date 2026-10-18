// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_released_values_claim_is_excluded : given.a_replay
{
    bool _excluded;
    bool _refusing;

    async Task Establish()
    {
        await _claims.Save("old", _definition, 0, Hash("value"));
        Event(0, "old", "Removed");
        Event(1, "new", "Claimed", "value");
        _unique.Remove(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await _claims.Remove(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<string>(2));
                await _claims.Save("new", _definition, 2, Hash("live"));
                _excluded = true;
            });
        _replay = new(_sequence, _unique, _closed, _content, Microsoft.Extensions.Logging.Abstractions.NullLogger<ConstraintIndexReplay>.Instance, [new(_definition, EventSequenceNumber.First)], 2)
        {
            CatchUp = new(
                difference => _excluded && difference.Source?.Value == "new",
                _ => false,
                (_, _) =>
                {
                    _refusing = true;
                    return Task.CompletedTask;
                },
                _ =>
                {
                    _refusing = false;
                    return Task.CompletedTask;
                })
        };
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_clear_the_marker_that_refuses_governed_appends() => _refusing.ShouldBeFalse();
    [Fact] void should_release_the_write_barrier() => _replay.CatchUp!.Refusing.ShouldBeFalse();
    [Fact] void should_finish_the_group_without_writing_the_excluded_claim() => _result.IsCompleted.ShouldBeTrue();
    [Fact] async Task should_have_released_the_old_holder() => (await Values("old")).ShouldBeEmpty();
    [Fact] async Task should_preserve_the_live_claim() => (await Values("new")).ShouldContainOnly(Hash("live").Value);
}
