// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Placement.for_ObserverPlacementDirector;

public class when_no_newer_host_exists : given.observer_hosts
{
    void Establish() => _releases.AtLeastAsNewAsThisSilo(Arg.Any<IEnumerable<SiloAddress>>()).Returns([]);
    Task Because() => Place();
    [Fact] void should_fall_back_to_the_compatible_hosts() => new[] { _local, _other }.ShouldContain(_result);
}
