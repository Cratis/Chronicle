// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Log messages of the federation authority. None of them ever carries a token secret or key material.
/// </summary>
internal static partial class FederationAuthorityLogging
{
    [LoggerMessage(LogLevel.Information, "Created the federation authority with fingerprint {Fingerprint}")]
    internal static partial void AuthorityCreated(this ILogger<FederationAuthority> logger, string fingerprint);

    [LoggerMessage(LogLevel.Information, "Created federation enrollment {EnrollmentId} for subject '{Subject}' instance '{Instance}', expiring {ExpiresAt}")]
    internal static partial void EnrollmentCreated(this ILogger<FederationAuthority> logger, string enrollmentId, string subject, string instance, DateTimeOffset expiresAt);

    [LoggerMessage(LogLevel.Information, "Revoked federation enrollment {EnrollmentId}")]
    internal static partial void EnrollmentRevoked(this ILogger<FederationAuthority> logger, string enrollmentId);

    [LoggerMessage(LogLevel.Information, "Revoked federation certificate {Serial} of {Identity}")]
    internal static partial void CertificateRevoked(this ILogger<FederationAuthority> logger, string serial, string identity);

    [LoggerMessage(LogLevel.Information, "Issued federation certificate {Serial} to {Identity} for {Reason}, expiring {NotAfter}")]
    internal static partial void CertificateIssued(this ILogger<FederationAuthority> logger, string serial, string identity, DateTimeOffset notAfter, string reason);

    [LoggerMessage(LogLevel.Warning, "A federation enrollment request was refused")]
    internal static partial void EnrollmentRefused(this ILogger<FederationAuthority> logger);

    [LoggerMessage(LogLevel.Warning, "A federation renewal request was refused")]
    internal static partial void RenewalRefused(this ILogger<FederationAuthority> logger);

    [LoggerMessage(LogLevel.Warning, "A federation certificate request could not be honored")]
    internal static partial void CertificateRequestInvalid(this ILogger<FederationAuthority> logger);
}
