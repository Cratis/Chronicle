// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Observation.Placement.for_ObserverPlacementDirector.when_placing;

public class with_only_unknown_silos : given.a_director
{
    void Establish()
    {
        _context.GetCompatibleSilos(Arg.Any<PlacementTarget>()).Returns([_unknown]);
    }

    async Task Because() => _selected = await _director.OnAddActivation(new ObserverPlacementStrategy(), default, _context);

    [Fact] void should_place_on_an_eligible_silo() => _selected.ShouldEqual(_unknown);
}
