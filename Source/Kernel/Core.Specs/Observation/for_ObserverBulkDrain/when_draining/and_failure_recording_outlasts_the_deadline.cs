// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ObserverBulkDrain.when_draining;

public class and_failure_recording_outlasts_the_deadline : Specification
{
    readonly ObserverDetails _details = new(new("observer", "store", "namespace", "event-log"), ObserverType.Projection);
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource _recording = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _recordingStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ObserverBulkDrain _drain;
    Task _stop;

    void Establish()
    {
        var factory = Substitute.For<IGrainFactory>();
        var observer = Substitute.For<IObserver>();
        factory.GetGrain<IObserver>(_details.Key).Returns(observer);
        observer.PartitionFailed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>()).Returns(_ =>
        {
            _recordingStarted.SetResult();
            return _recording.Task;
        });
        _drain = new(factory, NullLogger<ObserverService>.Instance);
    }

    async Task Because()
    {
        _stop = _drain.Drain(
            _details,
            _ => Task.FromResult<IEnumerable<Storage.Sinks.FailedPartition>>([new("partition", 42UL)]),
            _deadline.Token);
        await _recordingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _deadline.CancelAsync();
        await _stop.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy()
    {
        _recording.TrySetResult();
        _deadline.Dispose();
    }

    [Fact] void should_return_without_throwing() => _stop.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_extend_the_deadline_for_failure_recording() => _recording.Task.IsCompleted.ShouldBeFalse();
}
