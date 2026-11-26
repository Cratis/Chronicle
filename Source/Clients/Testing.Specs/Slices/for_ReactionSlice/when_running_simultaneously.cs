// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;
using Cratis.Chronicle.Testing.Slices.for_ReactionSlice.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

public class when_running_simultaneously : a_reaction_slice<ControlledReactor>
{
    ReactorControl _control;
    Exception?[] _results;
    Exception _lateInput;
    Barrier _start;

    void Establish()
    {
        _start = new(3);
        _control = new();
        _scenario.Services.AddSingleton(_ => _control);
        _slice.GivenEvents(_source, new ReactorRouteRecorded("first"));
    }

    async Task Because()
    {
        var first = RunOnThread();
        var second = RunOnThread();
        _start.SignalAndWait();
        try
        {
            await _control.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            _lateInput = Catch.Exception(() => _slice.GivenEvents(_source, new ReactorRouteRecorded("late")));
            await Task.WhenAny(first, second).WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        }
        finally
        {
            _control.Release.TrySetResult();
            _results = await Task.WhenAll(first, second);
        }
    }

    void Destroy() => _start.Dispose();

    Task<Exception?> RunOnThread() => Task.Factory.StartNew(
        () =>
        {
            _start.SignalAndWait();

            return Catch.Exception(() => _slice.Run().GetAwaiter().GetResult());
        },
        CancellationToken.None,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default);

    [Fact] void should_complete_one_run() => _results.Count(error => error is null).ShouldEqual(1);
    [Fact] void should_reject_the_other_run_before_the_handler_finishes() => _results.Count(error => error is SliceAlreadyRun).ShouldEqual(1);
    [Fact] void should_reject_inputs_while_the_handler_is_running() => _lateInput.ShouldBeOfExactType<SliceAlreadyRun>();
    [Fact] void should_dispatch_the_batch_once() => _control.Inputs.Count.ShouldEqual(1);
}
