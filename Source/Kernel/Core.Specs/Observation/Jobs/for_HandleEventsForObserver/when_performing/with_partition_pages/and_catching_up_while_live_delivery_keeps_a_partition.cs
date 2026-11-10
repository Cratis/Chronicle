// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_partition_pages;

/// <summary>
/// A catch-up walks every partition at once, but only the module partition was registered as catching up. Live delivery
/// keeps the feature partition, whose event was appended after the walk was prepared, so the walk must leave it alone or
/// the event reaches the subscriber twice.
/// </summary>
public class and_catching_up_while_live_delivery_keeps_a_partition : given.a_partition_page_job_step
{
    void Establish()
    {
        _performState.SkipFailedPartitions = true;
        _observer.GetState().Returns(new ObserverState() with { CatchingUpPartitions = new HashSet<Key> { "module" } });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_deliver_the_partition_it_holds() => _pages.SelectMany(_ => _).Select(_ => _.Partition).ShouldContainOnly([(Key)"module"]);
    [Fact] void should_deliver_every_event_of_the_partition_it_holds() => _pages.SelectMany(_ => _).SelectMany(_ => _.Events).Select(_ => _.Context.SequenceNumber.Value).ShouldEqual([1UL, 3UL]);
}
