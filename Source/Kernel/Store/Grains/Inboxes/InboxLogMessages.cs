// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.Grains.Inboxes;

/// <summary>
/// Holds log messages for <see cref="Inbox"/>.
/// </summary>
public static partial class InboxLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "Forwarding event ({EventTypeId}) with sequence number from origin {SequenceNumber} for microservice '{MicroserviceId}' and tenant '{TenantId}'")]
    internal static partial void ForwardingEvent(this ILogger logger, TenantId tenantId, MicroserviceId microserviceId, EventTypeId eventTypeId, EventSequenceNumber sequenceNumber);
}
