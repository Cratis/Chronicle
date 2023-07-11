// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Clients;

internal static partial class ClientObserverSubscriberLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Observer {ObserverId} in microservice {MicroserviceId} for tenant {TenantId} received event of type {EventTypeId} in sequence {EventSequenceId} with sequence number {EventSequenceNumber}")]
    internal static partial void EventReceived(this ILogger<ClientObserverSubscriber> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventTypeId eventTypeId, EventSequenceId eventSequenceId, EventSequenceNumber eventSequenceNumber);
}
