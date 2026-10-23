// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Jobs.Placement.for_JobWorkerPlacementDirector.when_placing;

public class without_jobs_silos_and_any_silo_fallback : given.a_director
{
    void Establish()
    {
        _options.Clustering.Roles.Jobs = false;
        _siloRoles.GetRoles(_remote).Returns(new ClusterRoles { Jobs = false });
        _context.GetCompatibleSilos(Arg.Any<PlacementTarget>()).Returns([_local, _remote]);
    }

    async Task Because() => _selected = await _director.OnAddActivation(new JobWorkerPlacementStrategy(), default, _context);

    [Fact] void should_place_on_an_eligible_silo() => new[] { _local, _remote }.ShouldContain(_selected);
}
