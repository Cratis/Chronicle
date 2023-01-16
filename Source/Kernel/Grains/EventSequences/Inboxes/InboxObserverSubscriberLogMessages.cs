// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Inbox;

/// <summary>
/// Holds log messages for <see cref="Inbox"/>.
/// </summary>
internal static partial class InboxObserverSubscriberLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "Forwarding event ({EventName}-{EventTypeId}) with sequence number from origin {SequenceNumber} for microservice '{MicroserviceId}' and tenant '{TenantId}'")]
    internal static partial void ForwardingEvent(this ILogger<InboxObserverSubscriber> logger, TenantId tenantId, MicroserviceId microserviceId, EventTypeId eventTypeId, string eventName, EventSequenceNumber sequenceNumber);
}
