// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.Clients;

internal static partial class ClientObserverLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "Starting client observer {observerId} for microservice {microserviceId} on sequence {eventSequenceId} for tenant {tenantId}")]
    internal static partial void Starting(this ILogger<ClientObserver> logger, MicroserviceId microserviceId, ObserverId observerId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(1, LogLevel.Debug, "Client with connection id {connectionId} has disconnected - unsubscribing observer {observerId} for microservice {microserviceId} on sequence {eventSequenceId} for tenant {tenantId}")]
    internal static partial void ClientDisconnected(this ILogger<ClientObserver> logger, ConnectionId connectionId, MicroserviceId microserviceId, ObserverId observerId, EventSequenceId eventSequenceId, TenantId tenantId);
}
