// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Log messages of the peer side of federation enrollment. None of them ever carries an enrollment code, token secret or key material.
/// </summary>
internal static partial class FederationPeerEnrollmentLogging
{
    [LoggerMessage(LogLevel.Information, "Enrolled as {Identity} with certificate {Serial}, expiring {NotAfter}")]
    internal static partial void Enrolled(this ILogger<FederationPeerEnrollment> logger, string identity, string serial, DateTimeOffset notAfter);

    [LoggerMessage(LogLevel.Information, "Renewed the certificate of {Identity} as {Serial}, expiring {NotAfter}")]
    internal static partial void Renewed(this ILogger<FederationPeerEnrollment> logger, string identity, string serial, DateTimeOffset notAfter);

    [LoggerMessage(LogLevel.Warning, "The authority refused enrollment {EnrollmentId}")]
    internal static partial void EnrollmentWasRefused(this ILogger<FederationPeerEnrollment> logger, string enrollmentId);

    [LoggerMessage(LogLevel.Warning, "The authority refused to renew the certificate of {Identity}")]
    internal static partial void RenewalWasRefused(this ILogger<FederationPeerEnrollment> logger, string identity);
}
