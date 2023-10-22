// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation;

internal static partial class ObserverWorkerLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "Observer {ObserverId} is now active up for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void Active(this ILogger<ObserverWorker> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(1, LogLevel.Warning, "Partition {Partition} failed for event with sequence number {EventSequenceNumber} observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void PartitionFailed(this ILogger<ObserverWorker> logger, string partition, EventSequenceNumber eventSequenceNumber, ObserverId observerId, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId, MicroserviceId sourceMicroserviceId, TenantId sourceTenantId);

    [LoggerMessage(2, LogLevel.Debug, "Observer {ObserverId} was subscribed to type '{Type}' for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void SubscribedObserver(this ILogger<ObserverWorker> logger, ObserverId observerId, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId, string type);
}
