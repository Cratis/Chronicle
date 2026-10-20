// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_a_timeout_fence_is_reactivated : given.an_observer_with_a_superseded_client
{
    static readonly SiloAddress _supersededSiloAddress = SiloAddress.FromParsableString("127.0.0.1:11111@1");
    IEnumerable<DeliveryFence> _fences;

    async Task Because()
    {
        var key = _observerKey;
        var original = new DeliveryFenceTarget(typeof(IClientOwnedObserverSubscriber).AssemblyQualifiedName!, new ObserverSubscriberKey(key.ObserverId, key.EventStore, key.Namespace, key.EventSequenceId, "partition", _supersededSiloAddress.ToParsableString()).ToString(), _supersededClient.ConnectionId, _supersededSiloAddress.ToParsableString());
        await _observer.PartitionFailed("partition", 42UL, ["timeout"], string.Empty, FailureKind.Timeout, deliveryFence: new(DateTimeOffset.UtcNow.AddSeconds(300), [original]));
        await Reactivate();
        _fences = await _observer.GetDeliveryFences();
    }

    [Fact] void should_restore_the_original_identity_not_the_current_subscription() => _fences.Single().Targets.Single().ConnectionId.ShouldEqual(_supersededClient.ConnectionId.ToString());
    [Fact] void should_persist_the_original_identity_on_the_timeout_attempt() => _failedPartitionsStorage.State.Partitions.Single().LastAttempt.DeliveryFence!.Targets!.Single().ConnectionId.ShouldEqual(_supersededClient.ConnectionId.ToString());
}
