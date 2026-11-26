// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Slices.for_ReactionSlice;

public class when_delivering_multiple_batches : a_reaction_slice<BareEventRouteReactor>
{
    ReactionSliceOutcome<BareEventRouteReactor> _outcome;
    int _before;
    Exception _repeat;
    Exception _addition;

    void Establish()
    {
        object[] events = [new ReactorRouteRecorded("first")];
        _slice.GivenEvents(_source, events).GivenEvents("another-source", new ReactorRouteRecorded("second"));
        events[0] = new ReactorRouteRecorded("changed");
        _before = _scenario.Produced.Count;
    }

    async Task Because()
    {
        _outcome = await _slice.Run();
        _outcome.ShouldHaveProducedEventsInOrder(new ProducedRouteRecorded("first"), new ProducedRouteRecorded("second"))
            .ShouldHaveProducedEventsInAnyOrder(new ProducedRouteRecorded("second"), new ProducedRouteRecorded("first"))
            .Should(scenario => scenario.ShouldHaveProduced<ProducedRouteRecorded>(_source));
        _repeat = await Catch.Exception(_slice.Run);
        _addition = Catch.Exception(() => _slice.GivenEvents(_source, new ReactorRouteRecorded("third")));
    }

    [Fact] void should_defer_invocation_until_run() => _before.ShouldEqual(0);
    [Fact] void should_deliver_copied_batches_in_declaration_order() => _scenario.ProducedEvents.Count.ShouldEqual(2);
    [Fact] void should_expose_the_same_outcome_after_awaited_delivery() => ReferenceEquals(_slice.Then, _outcome).ShouldBeTrue();
    [Fact] void should_reject_a_repeat_run_without_duplicating_effects() => _repeat.ShouldBeOfExactType<SliceAlreadyRun>();
    [Fact] void should_reject_additional_batches() => _addition.ShouldBeOfExactType<SliceAlreadyRun>();
}
