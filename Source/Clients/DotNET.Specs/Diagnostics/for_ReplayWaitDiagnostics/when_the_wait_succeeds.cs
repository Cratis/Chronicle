// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Diagnostics.for_ReplayWaitDiagnostics;

public class when_the_wait_succeeds : Specification
{
    int _waits;
    int _reads;
    int _budgets;
    Exception? _error;

    async Task Because()
    {
        var sources = new ReplayWaitSources(
            () =>
            {
                _reads++;
                return Task.FromResult<IEnumerable<Job>>([]);
            },
            "SomeReactor",
            () =>
            {
                _reads++;
                return Task.FromResult(new ReactorState(new ReactorId("r"), ObserverRunningState.Active, true, 1UL, 2UL, 3UL));
            },
            "SomeReducer",
            () =>
            {
                _reads++;
                return Task.FromResult(new ReducerState(ObserverRunningState.Active, true, 1UL, 2UL, 3UL));
            },
            "SomeProjection",
            () =>
            {
                _reads++;
                return Task.FromResult(new ProjectionState(ObserverRunningState.Active, true, 1UL, 2UL, 3UL));
            });

        try
        {
            await ReplayWaitDiagnostics.Run(
                () =>
                {
                    _waits++;
                    return Task.CompletedTask;
                },
                sources,
                () =>
                {
                    _budgets++;
                    return new CancellationTokenSource();
                });
        }
        catch (Exception exception)
        {
            _error = exception;
        }
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_wait_exactly_once() => _waits.ShouldEqual(1);
    [Fact] void should_not_read_any_snapshot() => _reads.ShouldEqual(0);
    [Fact] void should_not_allocate_a_budget() => _budgets.ShouldEqual(0);
}
