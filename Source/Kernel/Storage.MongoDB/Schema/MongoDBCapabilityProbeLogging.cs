// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Structured operational logging for MongoDB capability probes.
/// </summary>
internal static partial class MongoDBCapabilityProbeLogging
{
    /// <summary>
    /// Reports an inconclusive probe without retaining connection details or raw server error text.
    /// </summary>
    [LoggerMessage(LogLevel.Warning, "MongoDB capability probe for {Capability} was inconclusive: {Reason} Startup will continue.")]
    internal static partial void CapabilityProbeInconclusive(this ILogger logger, string capability, string reason);

    /// <summary>
    /// Reports unsuccessful cleanup without masking the original capability result or disclosing connection details.
    /// </summary>
    [LoggerMessage(LogLevel.Warning, "MongoDB capability probe could not remove its temporary collection; remove chronicle_capability_probe with a schema identity.")]
    internal static partial void CapabilityProbeCleanupFailed(this ILogger logger);
}
