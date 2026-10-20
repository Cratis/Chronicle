// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_refusal_release_arrives_after_retirement : given.a_retired_observer
{
    async Task Because()
    {
        await _observer.ReleaseRefusedReplay();
        await _observer.ReleaseRefusedPartitionReplay("not-replaying");
    }

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_start_catchup() => CheckDidNotStartCatchupJob();
}
