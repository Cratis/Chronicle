// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_gauges;

public class and_the_failures_are_read_again_when_subscribing_to_all_events : given.an_observer_reporting_gauges
{
    ObserverGaugeEntry _entryBeforeSubscribing;

    async Task Establish()
    {
        // Registers the gauge for the subscribed type with one failed partition, so the count published by the next
        // subscription can only come from the failures it reads again.
        await Subscribe();
        await _observer.PartitionFailed("earlier", 10UL, ["Something went wrong"], "stack trace");
        _entryBeforeSubscribing = Entry!;

        var stored = new FailedPartitions();
        stored.AddFailedPartition("first", 12UL);
        stored.AddFailedPartition("second", 14UL);
        _failedPartitionsStorage.State = stored;
    }

    Task Because() => Subscribe();

    Task Subscribe() => _observer.SubscribeToAllEvents<NullObserverSubscriber>(ObserverType.Reactor, SiloAddress.Zero);

    [Fact] void should_keep_reporting_to_the_same_entry() => Entry.ShouldEqual(_entryBeforeSubscribing);
    [Fact] void should_report_the_failed_partitions_that_were_read() => Entry!.Snapshot.FailedPartitions.ShouldEqual(2);
}
