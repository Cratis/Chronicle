// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_failed_catch_up_group_is_refolded : given.a_replay
{
    readonly CancellationTokenSource _budget = new();
    bool _refusing;
    bool _expire;
    bool _refusedAfterRaise;

    async Task Establish()
    {
        await _claims.Save("old", _definition, 0, Hash("value"));
        Event(0, "old", "Removed");
        Event(1, "new", "Claimed", "value");
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("unavailable")));
        _replay = new(_sequence, _unique, _closed, _content, Microsoft.Extensions.Logging.Abstractions.NullLogger<ConstraintIndexReplay>.Instance, [new(_definition, EventSequenceNumber.First)], 2)
        {
            CatchUp = new(_ => false, _ => false, Refuse, Allow)
        };
        await _replay.Continue();
        _replay.RaiseCatchUpBoundary(3);
        _refusedAfterRaise = _replay.CatchUp.UnderRefusal;
        _expire = true;
    }

    async Task Because() => _result = await _replay.Continue(cancellationToken: _budget.Token);

    async Task Refuse(IEnumerable<ConstraintName> names, CancellationToken token)
    {
        _refusing = true;
        if (_expire) await _budget.CancelAsync();
    }

    Task Allow(CancellationToken token)
    {
        _refusing = false;
        return Task.CompletedTask;
    }

    void Destroy() => _budget.Dispose();

    [Fact] void should_retain_refusal_when_discarding_the_failed_groups() => _refusedAfterRaise.ShouldBeTrue();
    [Fact] void should_not_clear_refusal_when_the_resumed_claim_has_not_started() => _refusing.ShouldBeTrue();
    [Fact] void should_not_report_completion() => _result.IsCompleted.ShouldBeFalse();
    [Fact] async Task should_exercise_a_released_but_unclaimed_value() => (await Values("old")).ShouldBeEmpty();
}
