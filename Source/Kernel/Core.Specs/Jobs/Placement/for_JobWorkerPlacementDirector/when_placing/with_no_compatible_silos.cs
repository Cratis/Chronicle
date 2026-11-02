// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Jobs.Placement.for_JobWorkerPlacementDirector.when_placing;

public class with_no_compatible_silos : given.a_director
{
    Exception? _error;

    void Establish()
    {
        _context.GetCompatibleSilos(Arg.Any<PlacementTarget>()).Returns([]);
    }

    async Task Because() => _error = await Catch.Exception(() => _director.OnAddActivation(new JobWorkerPlacementStrategy(), default, _context));

    [Fact] void should_fail_placement() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_missing_role_and_setting() => _error!.Message.ShouldContain("Cratis:Chronicle:Clustering:Roles:Jobs");
}
