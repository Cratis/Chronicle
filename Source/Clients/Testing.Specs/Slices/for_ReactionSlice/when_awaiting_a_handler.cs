// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;
using Cratis.Chronicle.Testing.Slices.for_ReactionSlice.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

public class when_awaiting_a_handler : a_reaction_slice<ControlledReactor>
{
    ReactorControl _control;
    bool _completedBeforeRelease;
    Exception _then;
    Exception _repeat;

    void Establish()
    {
        _control = new();
        _scenario.Services.AddSingleton(_ => _control);
        _slice.GivenEvents(_source, new ReactorRouteRecorded("first"));
    }

    async Task Because()
    {
        var run = _slice.Run();
        await _control.Entered.Task;
        _completedBeforeRelease = run.IsCompleted;
        _then = Catch.Exception(() => _ = _slice.Then);
        _repeat = await Catch.Exception(_slice.Run);
        _control.Release.SetResult();
        var outcome = await run;
        outcome.ShouldNotHaveProducedEvents().Should(_ => _control.Inputs.ShouldContain("first"));
        _slice.Dispose();
        _slice.Dispose();
    }

    [Fact] void should_await_the_real_invocation() => _completedBeforeRelease.ShouldBeFalse();
    [Fact] void should_not_expose_then_while_running() => _then.ShouldBeOfExactType<SliceNotRun>();
    [Fact] void should_reject_a_concurrent_run() => _repeat.ShouldBeOfExactType<SliceAlreadyRun>();
    [Fact] void should_dispose_scenario_owned_dependencies_once() => _control.Disposals.ShouldEqual(1);
}
