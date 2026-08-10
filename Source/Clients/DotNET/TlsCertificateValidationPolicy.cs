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
    /// <returns><see langword="true"/> when either input explicitly opts into skipping validation; otherwise <see langword="false"/>.</returns>
    public static bool ShouldSkip(Tls tls, ChronicleConnectionString connectionString) =>
        tls.SkipCertificateValidation || connectionString.SkipTlsValidation;
}
