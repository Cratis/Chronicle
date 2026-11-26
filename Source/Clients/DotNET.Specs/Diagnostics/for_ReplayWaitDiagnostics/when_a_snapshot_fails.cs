// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Diagnostics.for_ReplayWaitDiagnostics;

public class when_a_snapshot_fails : Specification
{
    const string Sentinel = "SECRET-FAILURE-MESSAGE";
    readonly Exception _original = new TimeoutException("original");
    Exception? _error;

    async Task Because()
    {
        var sources = new ReplayWaitSources(
            () => throw new InvalidOperationException(Sentinel),
            "SomeReactor",
            () => Task.FromException<ReactorState>(new ArgumentException(Sentinel)),
            "SomeReducer",
            () => Task.FromResult(new ReducerState(ObserverRunningState.Active, true, 4UL, 5UL, 6UL)),
            "SomeProjection",
            () => Task.FromResult(new ProjectionState(ObserverRunningState.Active, true, 7UL, 8UL, 9UL)));

        try
        {
            await ReplayWaitDiagnostics.Run(() => Task.FromException(_original), sources);
        }
        catch (Exception exception)
        {
            _error = exception;
        }
    }

    [Fact] void should_mark_the_synchronous_failure() => _error!.Message.ShouldContain("snapshot failed: InvalidOperationException");
    [Fact] void should_mark_the_faulted_task() => _error!.Message.ShouldContain("snapshot failed: ArgumentException");
    [Fact] void should_keep_the_reducer_snapshot() => _error!.Message.ShouldContain("reducer SomeReducer: state=Active");
    [Fact] void should_keep_the_projection_snapshot() => _error!.Message.ShouldContain("projection SomeProjection: state=Active");
    [Fact] void should_not_include_the_failure_message() => _error!.Message.ShouldNotContain(Sentinel);
    [Fact] void should_keep_the_original_as_inner_exception() => _error!.InnerException.ShouldBeSame(_original);
}
