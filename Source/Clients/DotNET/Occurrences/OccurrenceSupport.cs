// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Requires advertised occurrence support before sending any occurrence operation.
/// </summary>
internal static class OccurrenceSupport
{
    /// <summary>
    /// Refuses absent or unknown capabilities.
    /// </summary>
    /// <param name="connection">The kernel connection.</param>
    /// <exception cref="OccurrencesNotSupported">The kernel capability is absent or unknown.</exception>
    internal static void Require(IChronicleConnection connection)
    {
        if (connection is not IKernelCapabilities { CapabilitiesAreKnown: true } capabilities ||
            !capabilities.Capabilities.Contains(KernelCapabilities.Occurrences))
        {
            throw new OccurrencesNotSupported();
        }
    }
}
