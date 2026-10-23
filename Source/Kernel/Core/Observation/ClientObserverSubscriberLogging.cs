// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Logs abandoned client deliveries without treating late successes as recovery.
/// </summary>
internal static partial class ClientObserverSubscriberLogging
{
    [LoggerMessage(LogLevel.Warning, "Delivery expired before dispatch for {Observer} partition {Partition}")]
    internal static partial void DeliveryExpiredBeforeDispatch(this ILogger logger, ObserverKey observer, Key partition);

    [LoggerMessage(LogLevel.Information, "Late result for abandoned delivery for {Observer} partition {Partition}: {State}, late by {LateBy}")]
    internal static partial void LateResultForAbandonedDelivery(this ILogger logger, ObserverKey observer, Key partition, ObserverSubscriberState state, TimeSpan lateBy);

    [LoggerMessage(LogLevel.Warning, "Abandoned delivery hold expired for {Observer} partition {Partition}")]
    internal static partial void AbandonedDeliveryHoldExpired(this ILogger logger, ObserverKey observer, Key partition);

    [LoggerMessage(LogLevel.Warning, "Fence released by maximum hold for partition {Partition}; remote client work may still be running")]
    internal static partial void FenceReleasedByMaximumHold(this ILogger logger, Key partition);

    [LoggerMessage(LogLevel.Warning, "Could not drain original delivery target {SubscriberKey}; retaining fence until {ReleaseBy}")]
    internal static partial void OriginalDeliveryDrainFailed(this ILogger logger, Exception exception, string subscriberKey, DateTimeOffset releaseBy);
}
