// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Observation.Placement.for_ObserverPlacementDirector.when_placing;

public class with_no_eligible_silos : given.a_director
{
    Exception? _error;

    void Establish()
    {
        _options.Clustering.Roles.Observers = false;
        _siloRoles.GetRoles(_remote).Returns(new ClusterRoles { Observers = false });
        _context.GetCompatibleSilos(Arg.Any<PlacementTarget>()).Returns([_local, _remote]);
    }

    async Task Because() => _error = await Catch.Exception(() => _director.OnAddActivation(new ObserverPlacementStrategy(), default, _context));

    [Fact] void should_fail_placement() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_missing_role_and_setting() => _error!.Message.ShouldContain("Cratis:Chronicle:Clustering:Roles:Observers");
}
