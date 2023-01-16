// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Clients;

public static partial class ClientObserverLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Starting client observer {observerId} for microservice {microserviceId} on sequence {eventSequenceId} for tenant {tenantId}")]
    internal static partial void Starting(this ILogger<ClientObserver> logger, MicroserviceId microserviceId, ObserverId observerId, EventSequenceId eventSequenceId, TenantId tenantId);
}
