// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Jobs.Placement.for_JobWorkerPlacementDirector.when_placing;

public class with_default_configuration_on_a_remote_silo : given.a_director
{
    void Establish()
    {
        _siloRoles.GetRoles(_remote).Returns(new ClusterRoles());
        _context.GetCompatibleSilos(Arg.Any<PlacementTarget>()).Returns([_remote]);
    }

    async Task Because() => _selected = await _director.OnAddActivation(new JobWorkerPlacementStrategy(), default, _context);

    [Fact] void should_place_on_an_eligible_silo() => _selected.ShouldEqual(_remote);
}
