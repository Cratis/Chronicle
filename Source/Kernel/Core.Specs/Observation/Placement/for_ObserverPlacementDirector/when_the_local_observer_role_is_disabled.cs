// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Placement.for_ObserverPlacementDirector;

public class when_the_local_observer_role_is_disabled : given.observer_hosts
{
    void Establish()
    {
        _options.Clustering.Roles.Observers = false;
        _releases.AtLeastAsNewAsThisSilo(Arg.Any<IEnumerable<SiloAddress>>()).Returns(call => call.Arg<IEnumerable<SiloAddress>>().ToArray());
    }
    Task Because() => Place();
    [Fact] void should_exclude_the_local_silo() => _result.ShouldEqual(_other);
}
