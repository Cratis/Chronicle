// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.Placement.for_ObserverPlacementDirector.when_placing;

public class with_default_configuration_on_every_silo : given.a_director
{
    void Establish()
    {
        _siloRoles.GetRoles(_remote).Returns(new ClusterRoles());
        _siloRoles.GetRoles(_unknown).Returns(new ClusterRoles());
    }

    async Task Because() => _selected = await _director.OnAddActivation(new ObserverPlacementStrategy(), default, _context);

    [Fact] void should_place_on_an_eligible_silo() => new[] { _local, _remote, _unknown }.ShouldContain(_selected);
}
