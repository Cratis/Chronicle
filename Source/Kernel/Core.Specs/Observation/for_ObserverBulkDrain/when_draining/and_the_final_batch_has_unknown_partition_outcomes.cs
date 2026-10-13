// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ObserverBulkDrain.when_draining;

public class and_the_final_batch_has_unknown_partition_outcomes : Specification
{
    readonly ObserverDetails _details = new(new("observer", "store", "namespace", "event-log"), ObserverType.Projection);
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource<IEnumerable<Storage.Sinks.FailedPartition>> _finalBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Storage.Sinks.FailedPartition[] _failures =
    [
        new("first", 12UL) { Reason = "MongoDB bulk write outcome unknown (MongoException)" },
        new("second", 14UL) { Reason = "MongoDB bulk write outcome unknown (MongoException)" }
    ];
    IObserver _observer;
    ObserverBulkDrain _drain;
    Task _stop;

    void Establish()
    {
        var factory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        factory.GetGrain<IObserver>(_details.Key).Returns(_observer);
        _drain = new(factory, NullLogger<ObserverService>.Instance);
    }

    async Task Because()
    {
        _stop = _drain.Drain(_details, _ => _finalBatch.Task, _deadline.Token);
        _finalBatch.SetResult(_failures);
        await _stop.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _deadline.Dispose();

    [Fact] void should_not_throw() => _stop.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] async Task should_record_all_unknown_partitions_before_returning() => await _observer.Received(1).PartitionsFailed(Arg.Is<IReadOnlyCollection<Storage.Sinks.FailedPartition>>(failures => failures.SequenceEqual(_failures)));
}
