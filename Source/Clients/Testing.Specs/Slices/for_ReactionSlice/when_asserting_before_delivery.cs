// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

public class when_asserting_before_delivery : a_reaction_slice<BareEventRouteReactor>
{
    Exception _error;

    async Task Because()
    {
        _error = Catch.Exception(() => _ = _slice.Then);
        var outcome = await _slice.Run();
        outcome.ShouldNotHaveProducedEvents();
    }

    [Fact] void should_require_successful_execution() => _error.ShouldBeOfExactType<SliceNotRun>();
    [Fact] void should_allow_an_empty_successful_run() => _slice.Then.ShouldNotBeNull();
}
