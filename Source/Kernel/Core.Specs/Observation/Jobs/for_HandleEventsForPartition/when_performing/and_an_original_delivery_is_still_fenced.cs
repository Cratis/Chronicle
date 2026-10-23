// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_an_original_delivery_is_still_fenced : given.a_performing_job_step
{
    readonly TaskCompletionSource _barrier = new();
    IReactorObserverSubscriber _original;
    DeliveryFence _fence;
    bool _held;

    void Establish()
    {
        var silo = SiloAddress.FromParsableString("127.0.0.1:11111@1");
        var key = new ObserverSubscriberKey(_performState.ObserverKey.ObserverId, _performState.ObserverKey.EventStore, _performState.ObserverKey.Namespace, _performState.ObserverKey.EventSequenceId, "some-partition", silo.ToParsableString());
        _fence = new("some-partition", DateTimeOffset.UtcNow.AddSeconds(300), [new(typeof(IReactorObserverSubscriber).AssemblyQualifiedName!, key.ToString(), "A", silo.ToParsableString())]);
        _observer.GetSubscription().Returns(new ObserverSubscription(_performState.ObserverKey.ObserverId, _performState.ObserverKey, [], typeof(IReactorObserverSubscriber), silo));
        _observer.GetDeliveryFences().Returns([_fence]);
        _original = Substitute.For<IReactorObserverSubscriber>();
        _original.Drain().Returns(_barrier.Task);
        _silo.AddProbe(_ => _original);
        _eventCursor.Current.Returns([AppendedEvent.EmptyWithEventSequenceNumber(first_event_sequence_number)]);
        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(ObserverSubscriberResult.Ok(0));
    }

    async Task Because()
    {
        var perform = _jobStep.InvokePerformStep(_performState);
        if (perform.IsCompleted)
        {
            var unexpected = await perform;
            throw new InvalidOperationException(unexpected.Match(result => System.Text.Json.JsonSerializer.Serialize(result), error => error.ToString()));
        }
        _held = !perform.IsCompleted;
        _ = _observerSubscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
        _barrier.SetResult();
        await perform;
    }

    [Fact] void should_hold_recovery_before_the_first_delivery() => _held.ShouldBeTrue();
    [Fact] void should_drain_the_original_target() => _original.Received(1).Drain();
    [Fact] void should_release_only_the_drained_fence_version() => _ = _observer.Received(1).ReleaseDeliveryFence(_fence.Partition, _fence.ReleaseBy);
}
