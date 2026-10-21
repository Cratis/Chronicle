// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Drains only original delivery identities, holding until the deadline when they cannot be proven drained.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="clock">The clock.</param>
/// <param name="logger">The logger.</param>
internal class AbandonedDeliveryDrain(IGrainFactory grainFactory, TimeProvider clock, ILogger logger)
{
    /// <summary>Captures the selected original activation and connection before dispatch.</summary>
    /// <param name="subscription">The dispatched subscription.</param>
    /// <param name="partition">The partition.</param>
    /// <param name="target">The selected original target.</param>
    /// <param name="context">The dispatch deadlines.</param>
    /// <returns>The original delivery identity, or null when fencing is disabled.</returns>
    internal static DeliveryFenceContext? Capture(ObserverSubscription subscription, Key partition, ObserverSubscriberTarget target, ObserverSubscriberContext context) =>
        context.AbandonAt is { } abandonAt && subscription.SubscriberType.IsAssignableTo(typeof(IDrainableObserverSubscriber))
            ? new(abandonAt, [new(subscription.SubscriberType.AssemblyQualifiedName!, subscription.GetSubscriberKeyFor(partition, target.SiloAddress).ToString(), target.ConnectedClient?.ConnectionId.ToString() ?? string.Empty, target.SiloAddress.ToParsableString())])
            : null;

    /// <summary>Checks that an original identity can be resolved without using a current subscription.</summary>
    /// <param name="target">The captured target.</param>
    /// <returns>Whether the identity is valid.</returns>
    internal static bool IsValid(DeliveryFenceTarget target)
    {
        try
        {
            var type = Type.GetType(target.SubscriberType, throwOnError: false);
            var key = ObserverSubscriberKey.Parse(target.SubscriberKey);
            return type?.IsAssignableTo(typeof(IDrainableObserverSubscriber)) == true &&
                !string.IsNullOrWhiteSpace(target.ConnectionId) &&
                SiloAddress.FromParsableString(target.SiloAddress) != SiloAddress.Zero &&
                string.Equals(key.SiloAddress, target.SiloAddress, StringComparison.Ordinal);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Waits for original targets to drain or for the maximum hold to expire.</summary>
    /// <param name="fence">The captured fence.</param>
    /// <param name="cancellationToken">Cancels waiting without releasing the fence.</param>
    /// <returns>A task representing the bounded wait.</returns>
    internal async Task Until(DeliveryFence fence, CancellationToken cancellationToken)
    {
        var drained = fence.Targets.Count > 0 && fence.Targets.All(IsValid);
        if (drained)
        {
            // Start every original target's barrier before waiting. Never resolve from a new subscription.
            var drains = fence.Targets.Select(target => DrainOriginal(target, fence.ReleaseBy, cancellationToken)).ToArray();
            drained = (await Task.WhenAll(drains)).All(result => result);
        }
        if (!drained && clock.GetUtcNow() < fence.ReleaseBy)
        {
            await Task.Delay(fence.ReleaseBy - clock.GetUtcNow(), clock, cancellationToken);
        }
        if (!drained || clock.GetUtcNow() >= fence.ReleaseBy)
        {
            logger.FenceReleasedByMaximumHold(fence.Partition);
        }
    }

    async Task<bool> DrainOriginal(DeliveryFenceTarget target, DateTimeOffset releaseBy, CancellationToken cancellationToken)
    {
        if (clock.GetUtcNow() >= releaseBy) return false;
        while (clock.GetUtcNow() < releaseBy)
        {
            try
            {
                var subscriber = (IDrainableObserverSubscriber)grainFactory.GetGrain(Type.GetType(target.SubscriberType, throwOnError: true)!, target.SubscriberKey);
                var drain = subscriber.Drain();

                // Placement failure, dead silo, transport failure and timeout are not successful drains.
                await drain.WaitAsync(releaseBy - clock.GetUtcNow(), clock, cancellationToken);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TimeoutException exception)
            {
                logger.OriginalDeliveryDrainFailed(exception, target.SubscriberKey, releaseBy);
                var remaining = releaseBy - clock.GetUtcNow();
                if (remaining <= TimeSpan.Zero) return false;
                await Task.Delay(remaining < TimeSpan.FromSeconds(1) ? remaining : TimeSpan.FromSeconds(1), clock, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.OriginalDeliveryDrainFailed(exception, target.SubscriberKey, releaseBy);
                return false;
            }
        }
        return false;
    }
}
