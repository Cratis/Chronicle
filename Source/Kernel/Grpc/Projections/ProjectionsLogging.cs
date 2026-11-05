// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Log messages for projection registration.
/// </summary>
internal static partial class ProjectionsLogging
{
    /// <summary>
    /// Logs a declaration tracking failure without failing registration.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The tracking failure.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="connectionId">The declaring connection.</param>
    [LoggerMessage(LogLevel.Warning, "Failed recording projection declarations for event store {EventStore} from connection {ConnectionId}; registration will continue")]
    internal static partial void FailedRecordingDeclarations(this ILogger<Projections> logger, Exception exception, string eventStore, string connectionId);

    /// <summary>
    /// Logs that the signature of a registration could not be computed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(LogLevel.Warning, "Failed computing the signature of a projection registration; it will be registered without sharing the work with identical ones")]
    internal static partial void FailedComputingRegistrationSignature(this ILogger<Projections> logger, Exception exception);
}
