// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Connections;

/// <summary>
/// Converter methods for <see cref="ConnectedClient"/>.
/// </summary>
public static class ConnectedClientConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="ConnectedClient"/>.
    /// </summary>
    /// <param name="connectedClient"><see cref="ConnectedClient"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Kernel.Contracts.Clients.ConnectedClient ToContract(this ConnectedClient connectedClient)
    {
        return new()
        {
            ConnectionId = connectedClient.ConnectionId,
            Version = connectedClient.Version,
            IsRunningWithDebugger = connectedClient.IsRunningWithDebugger
        };
    }

    /// <summary>
    /// Convert a collection of see cref="ConnectedClient"/> to contract version.
    /// </summary>
    /// <param name="connectedClients">Collection of <see cref="ConnectedClient"/> to convert.</param>
    /// <returns>Collection of converted contract versions.</returns>
    public static IEnumerable<Kernel.Contracts.Clients.ConnectedClient> ToContract(this IEnumerable<ConnectedClient> connectedClients) =>
        connectedClients.Select(ToContract);

    /// <summary>
    /// Convert to kernel version of <see cref="ConnectedClient"/>.
    /// </summary>
    /// <param name="connectedClient"><see cref="Kernel.Contracts.Clients.ConnectedClient"/> to convert.</param>
    /// <returns>Converted kernel version.</returns>
    public static ConnectedClient ToKernel(this Kernel.Contracts.Clients.ConnectedClient connectedClient)
    {
        return new()
        {
            ConnectionId = connectedClient.ConnectionId,
            Version = connectedClient.Version,
            IsRunningWithDebugger = connectedClient.IsRunningWithDebugger
        };
    }
}
