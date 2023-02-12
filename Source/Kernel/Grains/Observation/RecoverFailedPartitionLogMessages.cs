// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation;

internal static partial class RecoverFailedPartitionLogMessages
{
    [LoggerMessage(8000, LogLevel.Warning, "Recovery error on event '{EventSequenceNumber}' for observer '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}'.  This event has failed {AttemptsOnCurrent} times and the partition a total of {TotalAttempts} times.")]
    internal static partial void SubscriberEventProcessingFailed(this ILogger<RecoverFailedPartition> logger, EventSequenceNumber eventSequenceNumber, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, int attemptsOnCurrent, int totalAttempts);
    
    [LoggerMessage(8001, LogLevel.Warning, "Unable to retrieve SubscriberSubscription observer '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}'.  ObserverKey or ObserverId is unavailable.")]
    internal static partial void UnableToGetSubscriberSubscription(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId);
    
    [LoggerMessage(8002, LogLevel.Information, "Recovery requested for observer '{ObserverId}' partition '{PartitionId}' sequence '{EventSequenceId}' in microservice '{MicroserviceId}' with tenant '{TenantId}' from position '{EventSequenceNumber}'")]
    internal static partial void RecoveryRequested(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, EventSequenceNumber eventSequenceNumber);
    
    [LoggerMessage(8003, LogLevel.Information, "Reset requested for observer '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}'.")]
    internal static partial void ResetRequested(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId);

    [LoggerMessage(8004, LogLevel.Information, "Recovery Processing Triggered for '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}' from Event '{EventSequenceNumber}'.")]
    internal static partial void RecoveryProcessingTriggered(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, EventSequenceNumber eventSequenceNumber);
    
    [LoggerMessage(8005, LogLevel.Information, "Recovery Processing Completed for '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}' on Event '{EventSequenceNumber}'.")]
    internal static partial void ProcessingCompleted(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(8006, LogLevel.Warning, "Recovery Processing Incomplete for '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}'. Stopped on Event '{EventSequenceNumber}'.")]
    internal static partial void ProcessingIncomplete(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, EventSequenceNumber eventSequenceNumber);
    
    [LoggerMessage(8007, LogLevel.Warning, "Processing Scheduled for '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}'. Scheduled for '{ScheduledTime}' with Event {EventSequenceNumber}.")]
    internal static partial void ProcessingScheduled(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, TimeSpan scheduledTime, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(8008, LogLevel.Warning, "Request to schedule recover for '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}' was ignored.  Recovery has not been initialised.")]
    internal static partial void ProcessingScheduleIgnored(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId);

    [LoggerMessage(8009, LogLevel.Information, "Recovery process for '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}' received Event '{EventSequenceNumber}' for processing.")]
    internal static partial void ReceivedEventForProcessing(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, EventSequenceNumber eventSequenceNumber);
    
    [LoggerMessage(8010, LogLevel.Information, "Recovery process for '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}' ignored Event '{EventSequenceNumber}' for processing as the next event to process is '{NextToProcess}'.")]
    internal static partial void EventForProcessingIgnored(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, EventSequenceNumber eventSequenceNumber, EventSequenceNumber nextToProcess);

    [LoggerMessage(8011, LogLevel.Warning, "Event '{EventSequenceNumber}' processed successfully on failing partition for observer '{ObserverId}' partition '{PartitionId}' for sequence '{EventSequenceId}' for microservice '{MicroserviceId}' and tenant '{TenantId}'.  The next event to process is {NextToProcess}.")]
    internal static partial void SubscriberEventProcessed(this ILogger<RecoverFailedPartition> logger, EventSequenceNumber eventSequenceNumber, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, EventSequenceNumber nextToProcess);
    
    [LoggerMessage(8012, LogLevel.Information, "Catchup requested for observer '{ObserverId}' partition '{PartitionId}' sequence '{EventSequenceId}' in microservice '{MicroserviceId}' with tenant '{TenantId}' from position '{EventSequenceNumber}'")]
    internal static partial void CatchupRequested(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, EventSequenceNumber eventSequenceNumber);
    
    [LoggerMessage(8012, LogLevel.Error, "SubscriberSubscription info missing on observer '{ObserverId}' partition '{PartitionId}' sequence '{EventSequenceId}' in microservice '{MicroserviceId}' with tenant '{TenantId}' when trying to process event '{EventSequenceNumber}'")]
    internal static partial void MissingSubscriberSubscription(this ILogger<RecoverFailedPartition> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId partitionId, EventSequenceNumber eventSequenceNumber);
}