// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Decides whether a request must be rejected because it arrived on the federation port but does not address the federation service.
/// </summary>
/// <remarks>
/// The federation listener is an additional listener on the same application, so without this every endpoint, the
/// Workbench, the API and the client gRPC services among them, would be reachable on it. The caller supplies the local
/// port the connection was accepted on, never a client-supplied value such as a Host header.
/// </remarks>
public static class FederationPortPolicy
{
    /// <summary>
    /// The path prefix of the federation service.
    /// </summary>
    public const string ServicePathPrefix = "/Cratis.Chronicle.Contracts.Federation.";

    /// <summary>
    /// Determines whether a request must be rejected.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/> in effect.</param>
    /// <param name="localPort">The local port the connection was accepted on.</param>
    /// <param name="path">The request path.</param>
    /// <returns>True if the request must be rejected, false if it must be served.</returns>
    public static bool ShouldReject(ChronicleOptions options, int localPort, string? path)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Federation.Enabled || localPort != options.Federation.Port)
        {
            return false;
        }

        return path?.StartsWith(ServicePathPrefix, StringComparison.Ordinal) != true;
    }
}
