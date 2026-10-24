// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Delivery fence management for the observer.
/// </summary>
public partial class Observer
{
    readonly DeliveryFences _deliveryFences = new();

    /// <inheritdoc/>
    public Task<IEnumerable<DeliveryFence>> GetDeliveryFences() => Task.FromResult(_deliveryFences.Active(_clock.GetUtcNow()));

    /// <inheritdoc/>
    public async Task ReleaseDeliveryFence(Key partition, DateTimeOffset releaseBy)
    {
        if (_deliveryFences.Release(partition, releaseBy)) await WriteStateAsync();
    }

    async Task RestoreDeliveryFences()
    {
        if (State.DeliveryFences is { } persisted)
        {
            foreach (var fence in persisted)
            {
                _deliveryFences.Hold(new(fence.Partition, fence.ReleaseBy, fence.Targets));
            }
        }
        else if (observerDefinition.State.Owner == ObserverOwner.Client)
        {
            _deliveryFences.SeedFrom(Failures, await configurationProvider.GetAbandonedDeliveryHoldForObserver(_observerKey));
        }

        State = State with { DeliveryFences = DeliveryFenceSnapshot() };
    }

    Storage.Observation.DeliveryFenceState[] DeliveryFenceSnapshot() =>
        _deliveryFences.Active(_clock.GetUtcNow()).Select(fence => new Storage.Observation.DeliveryFenceState(fence.Partition, fence.ReleaseBy, fence.Targets)).ToArray();

    async Task RecordAbandonedDeliveryFence(FailedPartition failure, FailureKind kind, DeliveryFenceContext? context)
    {
        failure.LastAttempt.DeliveryFence = context;
        if (kind != FailureKind.Timeout) return;
        if (IsClientDelivery(context))
        {
            await HoldAbandonedDelivery(failure.Partition, context);
        }
    }

    bool IsClientDelivery(DeliveryFenceContext? context) =>
        _subscription.SubscriberType.IsAssignableTo(typeof(IAmOwnedByClient)) || context is not null;

    Task<TimeSpan> GetClientDeliveryHold() =>
        _subscription.SubscriberType.IsAssignableTo(typeof(IAmOwnedByClient))
            ? configurationProvider.GetAbandonedDeliveryHoldForObserver(_observerKey)
            : Task.FromResult(TimeSpan.Zero);

    async Task<bool> CanDispatchDelivery(Key partition, AppendedEvent[] events)
    {
        if (events.Length == 0) return false;
        if (!_deliveryFences.IsHeld(partition, _clock.GetUtcNow())) return true;
        await PartitionFailed(partition, events[0].Context.SequenceNumber, ["Previous abandoned delivery is still fenced"], string.Empty);
        return false;
    }

    async Task HoldAbandonedDelivery(Key partition, DeliveryFenceContext? context)
    {
        var hold = await configurationProvider.GetAbandonedDeliveryHoldForObserver(_observerKey);
        if (hold <= TimeSpan.Zero) return;
        var known = context?.Targets is { Count: > 0 } targets && targets.All(AbandonedDeliveryDrain.IsValid);
        if (_deliveryFences.Hold(new(
            partition,
            known && context!.ReleaseBy is { } releaseBy ? releaseBy : _clock.GetUtcNow() + hold,
            known ? context!.Targets! : [])))
        {
            await WriteStateAsync();
        }
    }
}
