// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Placement.for_ObserverPlacementDirector;

public class when_adding_an_activation : given.observer_hosts
{
    void Establish() => _releases.AtLeastAsNewAsThisSilo(Arg.Any<IEnumerable<SiloAddress>>()).Returns([_other]);
    Task Because() => Place();
    [Fact] void should_prefer_the_newer_release() => _result.ShouldEqual(_other);
}
