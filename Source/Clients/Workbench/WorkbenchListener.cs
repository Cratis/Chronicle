// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Chronicle.Workbench;

/// <summary>
/// Configures the network listener for the standalone Workbench from <see cref="ChronicleWorkbenchOptions"/>.
/// </summary>
internal static class WorkbenchListener
{
    /// <summary>
    /// Check whether a configured bind address is valid.
    /// </summary>
    /// <param name="address">The configured address; empty means all interfaces.</param>
    /// <returns>True if valid, false if not.</returns>
    internal static bool IsValidAddress(string? address) =>
        string.IsNullOrEmpty(address) || IsLocalhost(address) || IPAddress.TryParse(address, out _);

    /// <summary>
    /// Configure the Kestrel listener for the Workbench.
    /// </summary>
    /// <param name="options"><see cref="KestrelServerOptions"/> to configure.</param>
    /// <param name="workbenchOptions"><see cref="ChronicleWorkbenchOptions"/> holding the port and address.</param>
    internal static void Configure(KestrelServerOptions options, ChronicleWorkbenchOptions workbenchOptions)
    {
        if (string.IsNullOrEmpty(workbenchOptions.Address))
        {
            options.ListenAnyIP(workbenchOptions.Port);
            return;
        }

        if (IsLocalhost(workbenchOptions.Address))
        {
            options.ListenLocalhost(workbenchOptions.Port);
            return;
        }

        options.Listen(IPAddress.Parse(workbenchOptions.Address), workbenchOptions.Port);
    }

    static bool IsLocalhost(string address) => address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
}
