// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Log messages of the federation policies. None of them ever carries a certificate or key material.
/// </summary>
internal static partial class FederationPoliciesLogging
{
    [LoggerMessage(LogLevel.Information, "Set the {Direction} federation policy of subject '{Subject}'")]
    internal static partial void PolicySet(this ILogger<FederationPolicies> logger, string subject, FederationDirection direction);

    [LoggerMessage(LogLevel.Information, "Suspended the federation policy of subject '{Subject}'")]
    internal static partial void PolicySuspended(this ILogger<FederationPolicies> logger, string subject);

    [LoggerMessage(LogLevel.Information, "Resumed the federation policy of subject '{Subject}'")]
    internal static partial void PolicyResumed(this ILogger<FederationPolicies> logger, string subject);

    [LoggerMessage(LogLevel.Information, "Revoked the federation policy of subject '{Subject}'")]
    internal static partial void PolicyRevoked(this ILogger<FederationPolicies> logger, string subject);

    [LoggerMessage(LogLevel.Information, "Revoked the {Direction} direction of the federation policy of subject '{Subject}'")]
    internal static partial void PolicyDirectionRevoked(this ILogger<FederationPolicies> logger, string subject, FederationDirection direction);
}
