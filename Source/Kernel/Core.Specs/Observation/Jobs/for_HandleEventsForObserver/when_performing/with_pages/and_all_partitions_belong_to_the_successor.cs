// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_all_partitions_belong_to_the_successor : given.a_page_job_step
{
    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_deliver_all_partitions_without_a_frozen_exclusion() => _pages.SelectMany(_ => _).Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([1UL, 2UL, 3UL]);
}
