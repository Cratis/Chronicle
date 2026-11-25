// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Releases.for_DormantObserverFloor;

public class when_every_silo_runs_an_earlier_release : given.a_cluster
{
    bool _result;

    void Establish() => Cluster(0, 0, (new SemanticVersion(19, 172, 0), 3));

    void Because() => _result = DormantObserverFloor.IsReached(_releases);

    [Fact] void should_not_be_reached() => _result.ShouldBeFalse();
}
