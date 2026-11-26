// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Diagnostics.for_ReplayWaitDiagnostics;

public class when_the_snapshot_budget_expires : Specification
{
    readonly Exception _original = new TaskCanceledException("original");
    readonly CancellationTokenSource _budget = new();
    readonly TaskCompletionSource<IEnumerable<Job>> _jobs = new();
    Exception? _error;
    string _messageBeforeLateFailure = string.Empty;

    async Task Because()
    {
        var sources = new ReplayWaitSources(
            () => _jobs.Task,
            "SomeReactor",
            () => Task.FromResult(new ReactorState(new ReactorId("r"), ObserverRunningState.Active, true, 1UL, 2UL, 3UL)),
            "SomeReducer",
            () => Task.FromResult(new ReducerState(ObserverRunningState.Active, true, 4UL, 5UL, 6UL)),
            "SomeProjection",
            () => Task.FromResult(new ProjectionState(ObserverRunningState.Active, true, 7UL, 8UL, 9UL)));

        var run = ReplayWaitDiagnostics.Run(() => Task.FromException(_original), sources, () => _budget);
        await _budget.CancelAsync();

        try
        {
            await run;
        }
        catch (Exception exception)
        {
            _error = exception;
        }

        _messageBeforeLateFailure = _error!.Message;
        _jobs.SetException(new InvalidOperationException("late"));
        await Catch.Exception(() => _jobs.Task);
    }

    [Fact] void should_mark_the_incomplete_reader_as_timed_out() => _messageBeforeLateFailure.ShouldContain("snapshot timed out");
    [Fact] void should_keep_the_completed_readers() => _messageBeforeLateFailure.ShouldContain("reactor SomeReactor");
    [Fact] void should_not_change_the_error_when_the_late_reader_fails() => _error!.Message.ShouldEqual(_messageBeforeLateFailure);
    [Fact] void should_keep_the_original_as_inner_exception() => _error!.InnerException.ShouldBeSame(_original);
}
