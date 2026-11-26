// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;
using Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_asserting_produced_events_absent;

public class when_recording_is_disabled : a_reaction_slice<AssertionReactor>
{
    ReactorScenario<AssertionReactor> _custom;
    Exception _error;

    void Establish() => _custom = new(_defaults, sideEffectHandlers: Substitute.For<IReactorSideEffectHandlers>());

    void Because() => _error = Catch.Exception(() =>
    {
        _custom.ShouldHaveProducedEventsInOrder();
        _custom.ShouldHaveProducedEventsInAnyOrder();
        _custom.ShouldNotHaveProducedEvents();
    });

    void Destroy() => _custom.Dispose();

    [Fact] void should_preserve_the_existing_empty_recording_behavior() => _error.ShouldBeNull();
}
