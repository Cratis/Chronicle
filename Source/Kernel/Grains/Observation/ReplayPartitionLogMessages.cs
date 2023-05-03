// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation;

internal static partial class ReplayPartitionLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Rewinding partition {Partition} to {SequenceNumber} for observer {ObserverId} for microservice '{MicroserviceId}' on sequence '{EventSequenceId}' for tenant '{TenantId}'.")]
    internal static partial void RewindingPartitionTo(this ILogger<ReplayPartition> logger, ObserverId observerId, MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId, EventSourceId partition, EventSequenceNumber sequenceNumber);
}
