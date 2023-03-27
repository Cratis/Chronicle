// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Holds log messages for <see cref="ObserverSupervisor"/>.
/// </summary>
internal static partial class ObserverSupervisorLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Subscribing observer {ObserverId} with subscriber type {SubscriberType} for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void Subscribing(this ILogger<ObserverSupervisor> logger, ObserverId observerId, Type subscriberType, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(1, LogLevel.Information, "Unsubscribing observer {ObserverId} for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void Unsubscribing(this ILogger<ObserverSupervisor> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(2, LogLevel.Information, "Rewinding observer {ObserverId} for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void Rewinding(this ILogger<ObserverSupervisor> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(3, LogLevel.Information, "Catching up observer {ObserverId} up for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void CatchingUp(this ILogger<ObserverSupervisor> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(4, LogLevel.Information, "Observer {ObserverId} is now active up for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void Active(this ILogger<ObserverSupervisor> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(5, LogLevel.Information, "Replaying observer {ObserverId} for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void Replaying(this ILogger<ObserverSupervisor> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(6, LogLevel.Information, "Offset is at tail for observer {ObserverId} for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void OffsetIsAtTail(this ILogger<ObserverSupervisor> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(7, LogLevel.Debug, "Clearing out failed partitions for observer {ObserverId} for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void ClearingFailedPartitions(this ILogger<ObserverSupervisor> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(8, LogLevel.Debug, "Clearing out recovering partitions for observer {ObserverId} for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'")]
    internal static partial void ClearingRecoveringPartitions(this ILogger<ObserverSupervisor> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId);

    [LoggerMessage(9, LogLevel.Debug, "Activating observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void Activating(this ILogger<ObserverSupervisor> logger, ObserverId observerId, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId, Guid sourceMicroserviceId, Guid sourceTenantId);

    [LoggerMessage(10, LogLevel.Trace, "Subscribing to stream for observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - stream {StreamId} - namespace {StreamNamespace}")]
    internal static partial void SubscribingToStream(this ILogger<ObserverSupervisor> logger, ObserverId observerId, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId, Guid streamId, string streamNamespace);

    [LoggerMessage(11, LogLevel.Trace, "Event of type {EventTypeId} received for observer {ObserverId} from sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}'")]
    internal static partial void EventReceived(this ILogger<ObserverSupervisor> logger, Guid eventTypeId, ObserverId observerId, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId);

    [LoggerMessage(12, LogLevel.Debug, "Deactivating observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void Deactivating(this ILogger<ObserverSupervisor> logger, ObserverId observerId, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId, MicroserviceId sourceMicroserviceId, TenantId sourceTenantId);

    [LoggerMessage(13, LogLevel.Warning, "Partition {Partition} failed for event with sequence number {EventSequenceNumber} observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void PartitionFailed(this ILogger<ObserverSupervisor> logger, string partition, EventSequenceNumber eventSequenceNumber, ObserverId observerId, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId, MicroserviceId sourceMicroserviceId, TenantId sourceTenantId);

    [LoggerMessage(14, LogLevel.Warning, "Fast forwarding from sequence number {EventSequenceNumber} to {TailEventSequenceNumber} for observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}'")]
    internal static partial void FastForwarding(this ILogger<ObserverSupervisor> logger, EventSequenceNumber eventSequenceNumber, EventSequenceNumber tailEventSequenceNumber, ObserverId observerId, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId);
}
