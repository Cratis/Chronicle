// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public Task ReleaseDeliveryFence(Key partition, DateTimeOffset releaseBy)
    {
        _deliveryFences.Release(partition, releaseBy);
        return Task.CompletedTask;
    }

    async Task HoldAbandonedDelivery(Key partition, DeliveryFenceContext? context)
    {
        var hold = await configurationProvider.GetAbandonedDeliveryHoldForObserver(_observerKey);
        if (hold <= TimeSpan.Zero) return;
        var known = context?.Targets is { Count: > 0 } targets && targets.All(AbandonedDeliveryDrain.IsValid);
        _deliveryFences.Hold(new(
            partition,
            known && context!.ReleaseBy is { } releaseBy ? releaseBy : _clock.GetUtcNow() + hold,
            known ? context!.Targets! : []));
    }
}
