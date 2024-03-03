// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Grains.Observation.Clients;

internal static partial class ClientObserverSubscriberLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Observer {ObserverId} in microservice {MicroserviceId} for tenant {TenantId} received event of type {EventTypeId} in sequence {EventSequenceId} with sequence number {EventSequenceNumber}")]
    internal static partial void EventReceived(this ILogger<ClientObserverSubscriber> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventTypeId eventTypeId, EventSequenceId eventSequenceId, EventSequenceNumber eventSequenceNumber);
}
