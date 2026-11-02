// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.EventSequences.Placement.for_EventSequencePlacementDirector.when_placing;

public class during_a_mixed_version_upgrade : given.a_director
{
    void Establish()
    {
        _options.Clustering.Roles.EventSequences = false;
        _siloRoles.GetRoles(_remote).Returns(new ClusterRoles { EventSequences = false });
    }

    async Task Because() => _selected = await _director.OnAddActivation(new EventSequencePlacementStrategy(), default, _context);

    [Fact] void should_place_on_an_eligible_silo() => _selected.ShouldEqual(_unknown);
}
