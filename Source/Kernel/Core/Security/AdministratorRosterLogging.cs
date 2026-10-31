// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

internal static partial class AdministratorRosterLogging
{
    [LoggerMessage(LogLevel.Warning, "Administrator roster event {Sequence} failed verification ({Class}); administrator privileges are suspended until operator recovery.")]
    internal static partial void AdministratorRosterVerificationFailed(this ILogger<AdministratorRoster> logger, EventSequenceNumber sequence, string @class);

    [LoggerMessage(LogLevel.Warning, "Administrator roster history cannot be verified; automatic upgrade/bootstrap grants are disabled. Use administratorRecovery.")]
    internal static partial void AutomaticAdministratorGrantsDisabled(this ILogger<AdministratorRoster> logger);

    [LoggerMessage(LogLevel.Error, "Administrator privilege initialization failed: {Cause}. Startup is stopped. Check System event-log storage health and permissions; stop competing roster writers if concurrency retries were exhausted, then restart. Use server-local administrator recovery only after storage is healthy if no active administrator remains.")]
    internal static partial void AdministratorInitializationFailed(this ILogger<AdministratorRoster> logger, string cause, Exception exception);

    [LoggerMessage(LogLevel.Error, "Guarded administrator roster append failed. Concurrency check performed: {ConcurrencyCheckPerformed}; errors: {Errors}. Check System event-log storage health and permissions before restarting.")]
    internal static partial void AdministratorAppendFailed(this ILogger<AdministratorRoster> logger, bool concurrencyCheckPerformed, string errors);
}
