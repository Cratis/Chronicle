// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

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
        !connectionString.DisableTls && skipTlsValidation && (connectionString.IsSrv || connectionString.ServerAddresses.Any(server => !IsLoopback(server.Host)));

    static bool IsLoopback(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (!IPAddress.TryParse(host.Trim('[', ']'), out var address)) return false;

        return address.Equals(IPAddress.IPv6Loopback) ||
            (address.AddressFamily == AddressFamily.InterNetwork && address.GetAddressBytes()[0] == 127);
    }
}
