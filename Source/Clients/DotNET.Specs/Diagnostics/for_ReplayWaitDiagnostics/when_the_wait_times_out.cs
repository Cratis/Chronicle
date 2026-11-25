// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Diagnostics.for_ReplayWaitDiagnostics;

public class when_the_wait_times_out : Specification
{
    readonly Exception _original = new TimeoutException("timed out");
    int _waits;
    Exception? _error;

    async Task Because()
    {
        var sources = new ReplayWaitSources(
            () => Task.FromResult<IEnumerable<Job>>(Array.Empty<Job>()),
            "SomeReactor",
            () => Task.FromResult(new ReactorState(new ReactorId("reactor-id"), ObserverRunningState.Replaying, true, 1UL, 2UL, 3UL)),
            "SomeReducer",
            () => Task.FromResult(new ReducerState(ObserverRunningState.Active, true, 4UL, 5UL, 6UL)),
            "SomeProjection",
            () => Task.FromResult(new ProjectionState(ObserverRunningState.Suspended, false, 7UL, 8UL, 9UL)));

        try
        {
            await ReplayWaitDiagnostics.Run(OriginalWait, sources);
        }
        catch (Exception exception)
        {
            _error = exception;
        }
    }

    async Task OriginalWait()
    {
        _waits++;
        await Task.Yield();
        await ThrowOriginal();
    }

    Task ThrowOriginal() => throw _original;

    [Fact] void should_throw_the_named_diagnostic_exception() => _error.ShouldBeOfExactType<ReplayWaitDidNotComplete>();
    [Fact] void should_keep_the_exact_original_as_inner_exception() => _error!.InnerException.ShouldBeSame(_original);
    [Fact] void should_keep_the_original_stack() => _error!.InnerException!.StackTrace!.ShouldContain(nameof(ThrowOriginal));
    [Fact] void should_not_retry_the_wait() => _waits.ShouldEqual(1);
    [Fact] void should_name_the_reactor() => _error!.Message.ShouldContain("reactor SomeReactor id=reactor-id: state=Replaying subscribed=True");
    [Fact] void should_name_the_reducer() => _error!.Message.ShouldContain("reducer SomeReducer: state=Active subscribed=True next=4 lastHandled=5 tail=6");
    [Fact] void should_name_the_projection() => _error!.Message.ShouldContain("projection SomeProjection: state=Suspended subscribed=False");
    [Fact] void should_say_there_are_no_jobs_when_there_are_none() => _error!.Message.ShouldContain("jobs: none outstanding");
}
