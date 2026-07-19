// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Clients;

/// <summary>
/// Converters between contracts and API models.
/// </summary>
internal static class ConnectedClientConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Clients.ConnectedClient"/> to a <see cref="ConnectedClient"/>.
    /// </summary>
    /// <param name="client">The connected client to convert.</param>
    /// <returns>The converted connected client.</returns>
    public static ConnectedClient ToApi(this Contracts.Clients.ConnectedClient client) =>
        new(client.SiloAddress, client.ConnectionId, client.Version, client.LastSeen, client.IsRunningWithDebugger, client.ProcessId, client.ProcessPath, client.MachineName, client.ClientType);

    /// <summary>
    /// Converts a collection of <see cref="Contracts.Clients.ConnectedClient"/> to a collection of <see cref="ConnectedClient"/>.
    /// </summary>
    /// <param name="clients">The connected clients to convert.</param>
    /// <returns>The converted connected clients.</returns>
    public static IEnumerable<ConnectedClient> ToApi(this IEnumerable<Contracts.Clients.ConnectedClient> clients) =>
        clients.Select(ToApi);
}
