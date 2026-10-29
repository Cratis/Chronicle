// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle;

/// <summary>
/// Resolves the effective TLS certificate validation policy for the .NET client.
/// </summary>
static class TlsCertificateValidationPolicy
{
    /// <summary>
    /// Resolves whether certificate validation should be skipped.
    /// </summary>
    /// <param name="tls"><see cref="Tls"/> options.</param>
    /// <param name="connectionString"><see cref="ChronicleConnectionString"/> options.</param>
    /// <returns>Whether certificate validation is skipped for this connection.</returns>
    /// <remarks>
    /// An explicit connection-string value wins, followed by an explicitly configured TLS option.
    /// Otherwise validation is required except for loopback-only development connections.
    /// When TLS is disabled there is no certificate to validate.
    /// </remarks>
    public static bool ShouldSkip(Tls tls, ChronicleConnectionString connectionString)
    {
        if (connectionString.DisableTls) return false;
        if (connectionString.HasExplicitSkipTlsValidation) return connectionString.SkipTlsValidation;
        if (tls.HasExplicitSkipCertificateValidation) return tls.SkipCertificateValidation;

        return connectionString.SkipTlsValidation;
    }
}
