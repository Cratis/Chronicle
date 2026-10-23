// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

using ReadModelsConfiguration = Cratis.Chronicle.Configuration.ReadModels;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_the_page_exceeds_the_cap : given.a_page_job_step
{
    protected override ReadModelsConfiguration CreateReadModelsConfig() => new() { MaxBatchSize = 2 };

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_split_the_page_into_two_calls() => _pages.Count.ShouldEqual(2);
    [Fact] void should_fill_the_first_call_to_the_cap() => _pages[0].Length.ShouldEqual(2);
    [Fact] void should_dispatch_the_remaining_event_once() => _pages[1].Single().Context.SequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
}
