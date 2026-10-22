// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_a_cleared_timeout_fence_is_reactivated : given.an_observer_with_a_superseded_client
{
    static readonly SiloAddress _originalSilo = SiloAddress.FromParsableString("127.0.0.1:11111@1");
    IClientOwnedObserverSubscriber _client;
    IEnumerable<DeliveryFence> _fences;
    DeliveryFenceTarget _original;
    readonly DateTimeOffset _releaseBy = DateTimeOffset.UtcNow.AddMinutes(5);

    async Task Because()
    {
        var key = _observerKey;
        _original = new(typeof(IClientOwnedObserverSubscriber).AssemblyQualifiedName!, new ObserverSubscriberKey(key.ObserverId, key.EventStore, key.Namespace, key.EventSequenceId, "partition", _originalSilo.ToParsableString()).ToString(), _supersededClient.ConnectionId, _originalSilo.ToParsableString());
        await _observer.PartitionFailed("partition", 42UL, ["timeout"], string.Empty, FailureKind.Timeout, context: new(DeliveryFence: new(_releaseBy, [_original])));
        await _observer.ClearFailedPartitions();
        await Reactivate();
        _fences = await _observer.GetDeliveryFences();
        _client = Substitute.For<IClientOwnedObserverSubscriber>();
        _silo.AddProbe(_ => _client);
        await _observer.Subscribe<IClientOwnedObserverSubscriber>(ObserverType.Reactor, [active_event_type], SiloAddress.Zero, _activeClient);
        await _observer.Handle("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(active_event_type, 42UL)]);
    }

    [Fact] void should_restore_the_original_target() => _fences.Single().Targets.ShouldContainOnly(_original);
    [Fact] void should_restore_the_original_deadline() => _fences.Single().ReleaseBy.ShouldEqual(_releaseBy);
    [Fact] void should_restore_the_partition() => _fences.Single().Partition.ShouldEqual((Key)"partition");
    [Fact] void should_refuse_the_new_delivery_because_it_is_fenced() => _failedPartitionsStorage.State.Partitions.Single().LastAttempt.Messages.ShouldContainOnly("Previous abandoned delivery is still fenced");
    [Fact] void should_not_dispatch_to_the_replacement() => _ = _client.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}
