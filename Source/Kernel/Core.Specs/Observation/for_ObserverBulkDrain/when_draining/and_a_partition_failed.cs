// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ObserverBulkDrain.when_draining;

public class and_a_partition_failed : Specification
{
    readonly ObserverDetails _details = new(new("observer", "store", "namespace", "event-log"), ObserverType.Projection);
    IObserver _observer;
    ObserverBulkDrain _drain;

    void Establish()
    {
        var factory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        factory.GetGrain<IObserver>(_details.Key).Returns(_observer);
        _drain = new(factory, NullLogger<ObserverService>.Instance);
    }

    async Task Because() => await _drain.Drain(
        _details,
        _ => Task.FromResult<IEnumerable<Storage.Sinks.FailedPartition>>([new("partition", 42UL) { Reason = "Unknown outcome" }]),
        CancellationToken.None);

    [Fact] async Task should_record_the_failure_before_returning() => await _observer.Received(1).PartitionFailed(Arg.Is<Key>(key => key.ToString() == "partition"), (EventSequenceNumber)42UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling);
}
