// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Releases.for_DormantObserverFloor;

public class when_every_silo_runs_the_floor_release : given.a_cluster
{
    bool _result;

    void Establish() => Cluster(0, 0, (ClusterReleaseBoundary.DormantObservers, 3));

    void Because() => _result = DormantObserverFloor.IsReached(_releases);

    [Fact] void should_be_reached() => _result.ShouldBeTrue();
}
