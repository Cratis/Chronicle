// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.Reducers.Clients;

internal static partial class ClientReducerLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Starting client reducer {observerId} for microservice {microserviceId} on sequence {eventSequenceId} for tenant {tenantId}")]
    internal static partial void Starting(this ILogger<ClientReducer> logger, MicroserviceId microserviceId, ObserverId observerId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(1, LogLevel.Information, "Client with connection id {connectionId} has disconnected - unsubscribing reducer {observerId} for microservice {microserviceId} on sequence {eventSequenceId} for tenant {tenantId}")]
    internal static partial void ClientDisconnected(this ILogger<ClientReducer> logger, ConnectionId connectionId, MicroserviceId microserviceId, ObserverId observerId, EventSequenceId eventSequenceId, TenantId tenantId);
}
