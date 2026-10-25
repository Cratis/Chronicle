// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Defines receiver logs that never include event content, reducer state or exception payloads.
/// </summary>
internal static partial class ChronicleReceiverLogging
{
    /// <summary>
    /// Logs the target and size of a decoded batch.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="observerType">The observer type.</param>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="partition">The partition.</param>
    /// <param name="eventCount">The batch size.</param>
    [LoggerMessage(0, LogLevel.Debug, "Receiving {ObserverType} {ObserverId} in {EventStore}/{Namespace}, partition {Partition}, {EventCount} events")]
    internal static partial void Receiving(this ILogger<ChronicleReceiver> logger, ObserverType observerType, string observerId, string eventStore, string @namespace, string partition, int eventCount);

    /// <summary>
    /// Logs the delivery status.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="status">The outcome status.</param>
    [LoggerMessage(1, LogLevel.Debug, "Completed delivery to {ObserverId} with {Status}")]
    internal static partial void Completed(this ILogger<ChronicleReceiver> logger, string observerId, DeliveryStatus status);

    /// <summary>
    /// Logs a rejection without its body or exception.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="status">The rejection status.</param>
    [LoggerMessage(2, LogLevel.Warning, "Delivery rejected with {Status}")]
    internal static partial void Rejected(this ILogger<ChronicleReceiver> logger, DeliveryStatus status);

    /// <summary>
    /// Logs an unexpected failure without exception or payload data.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(3, LogLevel.Error, "Delivery dispatch failed")]
    internal static partial void Failed(this ILogger<ChronicleReceiver> logger);
}
