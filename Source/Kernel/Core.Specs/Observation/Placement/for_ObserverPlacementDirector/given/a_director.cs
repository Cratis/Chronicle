// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Observation.Placement.for_ObserverPlacementDirector.given;

public class a_director : Cluster.given.a_role_placement
{
    protected ObserverPlacementDirector _director;

    protected IClusterReleases _releases;

    void Establish()
    {
        _releases = Substitute.For<IClusterReleases>();
        _releases.AtLeastAsNewAsThisSilo(Arg.Any<IEnumerable<SiloAddress>>()).Returns(call => call.Arg<IEnumerable<SiloAddress>>().ToArray());
        _director = new(_eligibility, _releases);
    }
}
