// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

internal static partial class StoreEntitlementsLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Event store entitlement observation could not be retained")]
    internal static partial void StoreEntitlementObservationFailed(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Event store entitlement snapshot refresh failed; retaining the last snapshot")]
    internal static partial void StoreEntitlementsRefreshFailed(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Event store entitlement broadcast failed; background polling will refresh the snapshot")]
    internal static partial void StoreEntitlementsBroadcastFailed(this ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Capability {Capability} is not granted to the caller (reason {Reason}); the query is allowed for this event store ({EventStore}): {CallerKind} issuer {Issuer}, subject hash {SubjectHash}")]
    internal static partial void StoreEntitlementAudited(this ILogger logger, string capability, string reason, string eventStore, string callerKind, string issuer, string subjectHash);

    [LoggerMessage(LogLevel.Information, "Event store entitlement audit tracking is full; further new entries are not tracked")]
    internal static partial void StoreEntitlementAuditFull(this ILogger logger);
}
