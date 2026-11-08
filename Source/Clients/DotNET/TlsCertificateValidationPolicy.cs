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
    /// <returns><see langword="true"/> when neither input asks for validation; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// An explicit false in either input requires validation, even if the other explicitly skips it.
    /// Otherwise an explicit true skips validation. When both settings are omitted, this release
    /// continues to skip validation; the next major release will require it by default.
    /// When TLS is disabled, certificate validation does not apply.
    /// </remarks>
    public static bool ShouldSkip(Tls tls, ChronicleConnectionString connectionString)
    {
        if (connectionString.DisableTls) return false;
        if (tls.HasExplicitSkipCertificateValidation && !tls.SkipCertificateValidation) return false;
        if (connectionString.HasExplicitSkipTlsValidation && !connectionString.SkipTlsValidation) return false;

        return true;
    }
}
