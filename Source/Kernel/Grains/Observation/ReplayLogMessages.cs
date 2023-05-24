// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation;

internal static partial class ReplayLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Starting replay for observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void Starting(this ILogger<Replay> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, MicroserviceId? sourceMicroserviceId, TenantId? sourceTenantId);

    [LoggerMessage(1, LogLevel.Information, "Stopping replay for observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void Stopping(this ILogger<Replay> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, MicroserviceId? sourceMicroserviceId, TenantId? sourceTenantId);

    [LoggerMessage(2, LogLevel.Information, "Observer {ObserverId} has replayed for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void Replayed(this ILogger<Replay> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, MicroserviceId? sourceMicroserviceId, TenantId? sourceTenantId);

    [LoggerMessage(3, LogLevel.Information, "Observer {ObserverId} is already replaying up for {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void AlreadyReplaying(this ILogger<Replay> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, MicroserviceId? sourceMicroserviceId, TenantId? sourceTenantId);

    [LoggerMessage(4, LogLevel.Information, "Event at {EventSequenceNumber} for Observer {ObserverId} is head of replay for event sequence {EventSequenceId}")]
    internal static partial void HeadOfReplay(this ILogger<Replay> logger, EventSequenceNumber eventSequenceNumber, ObserverId observerId, EventSequenceId eventSequenceId);

    [LoggerMessage(5, LogLevel.Error, "Error during replay for {ObserverId} on event sequence {EventSequenceId}")]
    internal static partial void ErrorDuringReplay(this ILogger<Replay> logger, ObserverId observerId, EventSequenceId eventSequenceId, Exception exception);
}
