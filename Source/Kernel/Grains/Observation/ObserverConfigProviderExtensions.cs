// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Configuration;
using Cratis.Chronicle.Concepts.Observation;
namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Extension methods for <see cref="IProvideConfigurationForObserver"/>.
/// </summary>
public static class ObserverConfigProviderExtensions
{
    /// <summary>
    /// Gets the timout for observer subscriber request.
    /// </summary>
    /// <param name="provider">The configuration provider.</param>
    /// <param name="key">The observer key.</param>
    /// <returns>The timeout.</returns>
    public static async Task<TimeSpan> GetSubscriberTimeoutForObserver(
        this IProvideConfigurationForObserver provider,
        ObserverKey key)
    {
        var config = await provider.GetFor(key);
        var timeout = TimeSpan.FromSeconds(config.SubscriberTimeoutInSeconds);
        if (timeout <= TimeSpan.Zero)
        {
            timeout = TimeSpan.FromSeconds(5);
        }

        return timeout;
    }
}
