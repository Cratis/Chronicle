// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Holds log messages for <see cref="MongoDBEventSequences"/>.
/// </summary>
public static partial class MongoDBEventSequencesLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Appending event with '{SequenceNumber}' as sequence number for sequence '{EventSequenceId}' in microservice '{MicroserviceId}' for tenant '{TenantId}'")]
    internal static partial void Appending(this ILogger logger, ulong sequenceNumber, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId);

    [LoggerMessage(1, LogLevel.Error, "Problem appending event with '{SequenceNumber}' as sequence number for sequence '{EventSequenceId}' in microservice '{MicroserviceId}' for tenant '{TenantId}'")]
    internal static partial void AppendFailure(this ILogger logger, ulong sequenceNumber, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId, Exception exception);
}
