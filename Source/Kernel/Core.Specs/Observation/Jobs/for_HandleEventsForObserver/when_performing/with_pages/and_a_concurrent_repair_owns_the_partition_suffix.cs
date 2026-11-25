// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_a_concurrent_repair_owns_the_partition_suffix : given.a_page_job_step
{
    void Establish() => _performState.ExcludedPartitions = ["module"];

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_leave_the_owned_suffix_to_its_partition_job() => _pages.SelectMany(_ => _).Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([2UL]);
}
