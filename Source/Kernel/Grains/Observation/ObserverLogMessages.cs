// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents the log messages for an observer.
/// </summary>
public static partial class ObserverLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Observer {ObserverId} is now active up for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void Active(this ILogger<Observer> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(1, LogLevel.Warning, "Partition {Partition} failed for event with sequence number {EventSequenceNumber} observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void PartitionFailed(this ILogger<Observer> logger, string partition, EventSequenceNumber eventSequenceNumber, ObserverId observerId, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId, MicroserviceId sourceMicroserviceId, TenantId sourceTenantId);
}
