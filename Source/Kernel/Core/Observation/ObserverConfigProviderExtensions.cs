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
}
