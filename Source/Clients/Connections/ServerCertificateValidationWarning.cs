// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Determines whether skipped server certificate validation warrants a warning.
/// </summary>
internal static class ServerCertificateValidationWarning
{
    /// <summary>
    /// Determines whether any configured target requires a warning.
    /// </summary>
    /// <param name="skipTlsValidation">Whether certificate validation is effectively skipped.</param>
    /// <param name="connectionString">The configured connection targets.</param>
    /// <returns>Whether skipped validation should be reported.</returns>
    internal static bool AppliesTo(bool skipTlsValidation, ChronicleConnectionString connectionString) =>
        !connectionString.DisableTls && skipTlsValidation;
}
