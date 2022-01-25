// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Holds log messages for <see cref="Observer"/>.
    /// </summary>
    public static partial class ObserverLogMesssages
    {
        [LoggerMessage(0, LogLevel.Information, "Subscription with identifier '{SubscriptionId}' is unavailable and can't be unsubscribed.")]
        internal static partial void UnsubscribeFailedSubscriptionUnavailable(this ILogger logger, Guid subscriptionId);
    }
}
