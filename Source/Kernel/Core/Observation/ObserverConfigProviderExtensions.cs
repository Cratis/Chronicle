// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for <see cref="IConfigurationForObserverProvider"/>.
/// </summary>
public static class ObserverConfigProviderExtensions
{
    /// <summary>
    /// Gets the effective hold for an abandoned client delivery.
    /// </summary>
    /// <param name="provider">The configuration provider.</param>
    /// <param name="key">The observer key.</param>
    /// <returns>The hold, or zero when fencing is disabled.</returns>
    public static async Task<TimeSpan> GetAbandonedDeliveryHoldForObserver(this IConfigurationForObserverProvider provider, ObserverKey key)
    {
        var config = await provider.GetFor(key);
        return config.AbandonedDeliveryHold <= 0 || config.SubscriberTimeout <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(Math.Max(config.SubscriberTimeout, config.AbandonedDeliveryHold));
    }

    /// <summary>
    /// Gets the timeout for observer subscriber request.
    /// </summary>
    /// <param name="provider">The configuration provider.</param>
    /// <param name="key">The observer key.</param>
    /// <returns>The timeout.</returns>
    /// <remarks>
    /// A configured zero - or anything below it - means waiting indefinitely, which is the escape hatch for a
    /// subscriber whose work legitimately has no upper bound.
    /// </remarks>
    public static async Task<TimeSpan> GetSubscriberTimeoutForObserver(
        this IConfigurationForObserverProvider provider,
        ObserverKey key)
    {
        var config = await provider.GetFor(key);
        return config.SubscriberTimeout <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(config.SubscriberTimeout);
    }

    /// <summary>
    /// Gets the most events to hand a subscriber in one call during catch-up, replay and partition recovery.
    /// </summary>
    /// <param name="provider">The <see cref="IConfigurationForObserverProvider"/>.</param>
    /// <param name="key">The <see cref="ObserverKey"/> of the observer.</param>
    /// <param name="subscriberType">The type of subscriber the events are handed to.</param>
    /// <returns>The bound, or zero when calls are not bounded.</returns>
    /// <remarks>
    /// Only client-owned and webhook subscribers are bounded: they are the ones a timeout guards, and kernel-owned
    /// subscribers keep the larger batches they are tuned for.
    /// </remarks>
    public static async Task<int> GetMaxEventsPerSubscriberCallForObserver(
        this IConfigurationForObserverProvider provider,
        ObserverKey key,
        Type subscriberType)
    {
        if (!subscriberType.IsAssignableTo(typeof(IAmOwnedByClient)) && !subscriberType.IsAssignableTo(typeof(Webhooks.IWebhookObserverSubscriber)))
        {
            return 0;
        }

        var config = await provider.GetFor(key);
        return Math.Max(0, config.MaxEventsPerSubscriberCall);
    }
}
