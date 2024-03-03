// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Grains.EventSequences.Inbox;

/// <summary>
/// Holds log messages for <see cref="Inbox"/>.
/// </summary>
internal static partial class InboxObserverSubscriberLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "Forwarding event ({EventName}-{EventTypeId}) with sequence number from origin {SequenceNumber} for microservice '{MicroserviceId}' and tenant '{TenantId}'")]
    internal static partial void ForwardingEvent(this ILogger<InboxObserverSubscriber> logger, TenantId tenantId, MicroserviceId microserviceId, EventTypeId eventTypeId, string eventName, EventSequenceNumber sequenceNumber);

    [LoggerMessage(1, LogLevel.Error, "Failed forwarding event ({EventTypeId}) with sequence number from origin {SequenceNumber} for microservice '{MicroserviceId}' and tenant '{TenantId}'")]
    internal static partial void FailedForwardingEvent(this ILogger<InboxObserverSubscriber> logger, TenantId tenantId, MicroserviceId microserviceId, EventTypeId eventTypeId, EventSequenceNumber sequenceNumber, Exception exception);
}
