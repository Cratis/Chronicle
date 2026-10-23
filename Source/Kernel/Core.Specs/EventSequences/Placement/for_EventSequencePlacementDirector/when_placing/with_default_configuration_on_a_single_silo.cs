// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.EventSequences.Placement.for_EventSequencePlacementDirector.when_placing;

public class with_default_configuration_on_a_single_silo : given.a_director
{
    void Establish()
    {
        _context.GetCompatibleSilos(Arg.Any<PlacementTarget>()).Returns([_local]);
    }

    async Task Because() => _selected = await _director.OnAddActivation(new EventSequencePlacementStrategy(), default, _context);

    [Fact] void should_place_on_an_eligible_silo() => _selected.ShouldEqual(_local);
}
