// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;
using Cratis.Chronicle.Testing.Slices.for_ReactionSlice.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

public class when_a_handler_fails : a_reaction_slice<ControlledReactor>
{
    Exception _failure;
    Exception _then;
    Exception _repeat;

    void Establish()
    {
        var control = new ReactorControl { Fail = true };
        control.Release.SetResult();
        _scenario.Services.AddSingleton(_ => control);
        _slice.GivenEvents(_source, new ReactorRouteRecorded("first"));
    }

    async Task Because()
    {
        _failure = await Catch.Exception(_slice.Run);
        _then = Catch.Exception(() => _ = _slice.Then);
        _repeat = await Catch.Exception(_slice.Run);
    }

    [Fact] void should_propagate_the_original_failure() => _failure.ShouldBeOfExactType<ControlledFailure>();
    [Fact] void should_not_expose_a_successful_outcome() => _then.ShouldBeOfExactType<SliceNotRun>();
    [Fact] void should_not_retry_and_duplicate_effects() => _repeat.ShouldBeOfExactType<SliceAlreadyRun>();
}
