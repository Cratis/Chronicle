// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Releases.for_DormantObserverFloor;

public class when_every_silo_runs_a_later_release : given.a_cluster
{
    bool _result;

    void Establish() => Cluster(0, 0, (new SemanticVersion(99, 0, 0), 3));

    void Because() => _result = DormantObserverFloor.IsReached(_releases);

    [Fact] void should_be_reached() => _result.ShouldBeTrue();
}
