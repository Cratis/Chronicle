// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Releases.for_DormantObserverFloor;

public class when_a_silo_has_not_published_its_release : given.a_cluster
{
    bool _result;

    void Establish() => Cluster(1, 0, (ClusterReleaseBoundary.DormantObservers, 2));

    void Because() => _result = DormantObserverFloor.IsReached(_releases);

    [Fact] void should_not_be_reached() => _result.ShouldBeFalse();
}
