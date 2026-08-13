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
    /// Both inputs default to skipping, so the Chronicle server's generated self-signed development
    /// certificate is accepted without any setup. They therefore combine with AND: validation is
    /// skipped only while neither input asks for it, and setting either one to <see langword="false"/>
    /// is enough to turn it on. Combining with OR would let the untouched input swallow an explicit
    /// <c>skipTlsValidation=false</c> and go on accepting any certificate.
    /// </remarks>
    public static bool ShouldSkip(Tls tls, ChronicleConnectionString connectionString) =>
        tls.SkipCertificateValidation && connectionString.SkipTlsValidation;
}
