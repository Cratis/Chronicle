// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Holds log messages for <see cref="Observer"/>.
/// </summary>
public static partial class ObserverLogMesssages
{
    [LoggerMessage(0, LogLevel.Information, "Subscribing for microservice ({MicroserviceId}) on sequence ({EventSequenceId}) for tenant ({TenantId})")]
    internal static partial void Subscribing(this ILogger logger, Guid microserviceId, Guid eventSequenceId, Guid tenantId);

    [LoggerMessage(1, LogLevel.Information, "Subscribing for microservice ({MicroserviceId}) on sequence ({EventSequenceId}) for tenant ({TenantId})")]
    internal static partial void Unsubscribing(this ILogger logger, Guid microserviceId, Guid eventSequenceId, Guid tenantId);

    [LoggerMessage(2, LogLevel.Information, "Rewinding for microservice ({MicroserviceId}) on sequence ({EventSequenceId}) for tenant ({TenantId})")]
    internal static partial void Rewinding(this ILogger logger, Guid microserviceId, Guid eventSequenceId, Guid tenantId);
}
