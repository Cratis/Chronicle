// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation;

internal static partial class ReplayLogMessages
{
    [LoggerMessage(0, LogLevel.Warning, "Starting replay for observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void Starting(this ILogger<Replay> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, MicroserviceId? sourceMicroserviceId, TenantId? sourceTenantId);

    [LoggerMessage(1, LogLevel.Warning, "Stopping replay for observer {ObserverId} for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void Stopping(this ILogger<Replay> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, MicroserviceId? sourceMicroserviceId, TenantId? sourceTenantId);

    [LoggerMessage(2, LogLevel.Warning, "Observer {ObserverId} has replayed for sequence {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void Replayed(this ILogger<Replay> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, MicroserviceId? sourceMicroserviceId, TenantId? sourceTenantId);

    [LoggerMessage(3, LogLevel.Warning, "Observer {ObserverId} is already replaying up for {EventSequenceId} for microservice '{MicroserviceId}' and tenant '{TenantId}' - observing source microservice '{SourceMicroserviceId}' and tenant '{SourceTenantId}'")]
    internal static partial void AlreadyReplaying(this ILogger<Replay> logger, ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, MicroserviceId? sourceMicroserviceId, TenantId? sourceTenantId);
}
