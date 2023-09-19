// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation;

internal static partial class ObserverLogMessages
{
    [LoggerMessage(0, LogLevel.Warning, "Partition {Partition} failed for event with sequence number {EventSequenceNumber} observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void PartitionFailed(this ILogger<Observer> logger, string partition, EventSequenceNumber eventSequenceNumber, ObserverId observerId, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId, MicroserviceId sourceMicroserviceId, TenantId sourceTenantId);
}
