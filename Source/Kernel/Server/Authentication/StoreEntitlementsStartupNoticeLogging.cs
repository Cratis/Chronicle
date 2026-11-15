// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Log messages for <see cref="StoreEntitlementsStartupNotice"/>.
/// </summary>
internal static partial class StoreEntitlementsStartupNoticeLogging
{
    [LoggerMessage(LogLevel.Information, "Event store entitlement audit mode is active. Review EventStoreAccessPreflight and configure explicit grants before enabling EnforceStoreEntitlements. Observations are bounded, best-effort evidence since activation, not policy.")]
    internal static partial void AuditMode(this ILogger<StoreEntitlementsStartupNotice> logger);

    [LoggerMessage(LogLevel.Warning, "Event store entitlement enforcement is active with no explicit grants. Ordinary application access will be denied until grants are configured.")]
    internal static partial void EmptyEnforcement(this ILogger<StoreEntitlementsStartupNotice> logger);

    [LoggerMessage(LogLevel.Warning, "Event store entitlement enforcement is active, but the configured grants could not be read to check that any exist.")]
    internal static partial void GrantsUnavailable(this ILogger<StoreEntitlementsStartupNotice> logger);
}
