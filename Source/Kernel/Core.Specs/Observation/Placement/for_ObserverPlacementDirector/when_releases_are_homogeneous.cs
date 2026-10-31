// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Placement.for_ObserverPlacementDirector;

public class when_releases_are_homogeneous : given.observer_hosts
{
    void Establish() => _releases.AtLeastAsNewAsThisSilo(Arg.Any<IEnumerable<SiloAddress>>()).Returns([_local, _other]);
    Task Because() => Place();
    [Fact] void should_preserve_existing_placement() => new[] { _local, _other }.ShouldContain(_result);
}
