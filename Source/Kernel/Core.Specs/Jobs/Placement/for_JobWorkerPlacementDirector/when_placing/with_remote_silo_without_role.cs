// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Jobs.Placement.for_JobWorkerPlacementDirector.when_placing;

public class with_remote_silo_without_role : given.a_director
{
    void Establish()
    {
        _options.Clustering.Roles.Jobs = false;
        _siloRoles.GetRoles(_remote).Returns(new ClusterRoles { Jobs = false });
    }

    async Task Because() => _selected = await _director.OnAddActivation(new JobWorkerPlacementStrategy(), default, _context);

    [Fact] void should_place_on_an_eligible_silo() => _selected.ShouldEqual(_unknown);
}
