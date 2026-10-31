// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ObserverBulkDrain.when_draining;

public class and_the_sink_reports_after_the_deadline : Specification
{
    readonly ObserverDetails _details = new(new("observer", "store", "namespace", "event-log"), ObserverType.Projection);
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource<IEnumerable<Storage.Sinks.FailedPartition>> _finalBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _outcomeObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IObserver _observer;
    ObserverBulkDrain _drain;
    bool _returnedWithoutOutcome;

    void Establish()
    {
        var factory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        factory.GetGrain<IObserver>(_details.Key).Returns(_observer);
        _drain = new(factory, NullLogger<ObserverService>.Instance);
    }

    async Task Because()
    {
        var stop = _drain.Drain(_details, _ => _finalBatch.Task, _deadline.Token);
        await _deadline.CancelAsync();
        await stop.WaitAsync(TimeSpan.FromSeconds(5));
        _returnedWithoutOutcome = !_finalBatch.Task.IsCompleted;
        _finalBatch.SetResult(Failures());
        await _outcomeObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _deadline.Dispose();

    IEnumerable<Storage.Sinks.FailedPartition> Failures()
    {
        yield return new("partition", 42UL) { Reason = "MongoDB bulk write outcome unknown (shutdown deadline)" };
        _outcomeObserved.TrySetResult();
    }

    [Fact] void should_not_wait_past_the_deadline_for_the_sink() => _returnedWithoutOutcome.ShouldBeTrue();
    [Fact] async Task should_not_attempt_failure_recording_after_the_deadline() => await _observer.DidNotReceive().PartitionFailed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>());
    [Fact] async Task should_not_attempt_batched_failure_recording_after_the_deadline() => await _observer.DidNotReceive().PartitionsFailed(Arg.Any<IReadOnlyCollection<Storage.Sinks.FailedPartition>>());
}
