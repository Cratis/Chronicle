// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_skipping_failed_partitions : given.a_page_job_step
{
    void Establish()
    {
        _performState.SkipFailedPartitions = true;
        _observer.GetFailedPartitionKeys().Returns([(Key)"module"]);
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_dispatch_events_for_a_failed_partition() => _pages.SelectMany(_ => _).Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([2UL]);
}
