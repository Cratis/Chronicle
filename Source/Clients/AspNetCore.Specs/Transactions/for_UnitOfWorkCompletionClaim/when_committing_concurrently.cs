// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkCompletionClaim;

public class when_committing_concurrently : Specification
{
    readonly UnitOfWorkHarness _harness = new();
    readonly TaskCompletionSource _appendStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<AppendManyResult> _append = new(TaskCreationOptions.RunContinuationsAsynchronously);
    UnitOfWorkCompletionResult _first;
    UnitOfWorkCompletionResult _second;
    UnitOfWorkCompletionResult _terminal;
    bool _secondWaited;

    void Establish() => _harness.Sequence.AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ =>
        {
            _appendStarted.SetResult();
            return _append.Task;
        });

    async Task Because() => await _harness.Run(async context =>
    {
        var claim = context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>()!.Claim();
        _harness.AddEvent();
        var first = claim.Commit();
        await _appendStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var second = claim.Commit();
        _secondWaited = !second.IsCompleted;
        _append.SetResult(AppendManyResult.Success(_harness.CorrelationId, []));
        _first = await first.WaitAsync(TimeSpan.FromSeconds(10));
        _second = await second.WaitAsync(TimeSpan.FromSeconds(10));
        _terminal = claim.Result!;
    });

    [Fact] void should_wait_for_the_in_flight_commit() => _secondWaited.ShouldBeTrue();
    [Fact] void should_append_once() => _harness.AppendCalls.ShouldEqual(1);
    [Fact] void should_commit() => _first.Outcome.ShouldEqual(UnitOfWorkCompletionOutcome.Committed);
    [Fact] void should_return_the_same_result() => _second.ShouldBeSame(_first);
    [Fact] void should_retain_the_terminal_result() => _terminal.ShouldBeSame(_first);
}
