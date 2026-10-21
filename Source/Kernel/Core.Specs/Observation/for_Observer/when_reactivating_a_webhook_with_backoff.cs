// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_reactivating_a_webhook_with_backoff : given.a_webhook_observer
{
    PartitionDeferral _persisted;

    async Task Establish()
    {
        await _observer.PartitionFailed("partition", 42UL, ["503"], "");
        _persisted = _failedPartitionsState.Partitions.Single().Deferral!;
    }

    async Task Because() => await Reactivate();

    [Fact] void should_not_resample_the_deadline() => _failedPartitionsStorage.State.Partitions.Single().Deferral.ShouldEqual(_persisted);
    [Fact] void should_not_consume_another_attempt() => _failedPartitionsStorage.State.Partitions.Single().Attempts.Count().ShouldEqual(1);
}
