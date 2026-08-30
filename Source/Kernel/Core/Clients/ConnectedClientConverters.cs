// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Converts connected clients into the read model the client queries answer with.
/// </summary>
internal static class ConnectedClientConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Clients.ConnectedClient"/> to a <see cref="ConnectedClientDetails"/>.
    /// </summary>
    /// <param name="client">The connected client to convert.</param>
    /// <returns>The converted connected client.</returns>
    internal static ConnectedClientDetails ToReadModel(this Contracts.Clients.ConnectedClient client) =>
        new(
            client.ConnectionId,
            client.SiloAddress,
            client.ConnectionId,
            client.Version,
            client.LastSeen,
            client.IsRunningWithDebugger,
            client.ProcessId,
            client.ProcessPath,
            client.MachineName,
            client.ClientType);

    /// <summary>
    /// Converts a collection of <see cref="Contracts.Clients.ConnectedClient"/> to <see cref="ConnectedClientDetails"/>.
    /// </summary>
    /// <param name="clients">The connected clients to convert.</param>
    /// <returns>The converted connected clients.</returns>
    internal static IEnumerable<ConnectedClientDetails> ToReadModel(this IEnumerable<Contracts.Clients.ConnectedClient> clients) =>
        [.. clients.Select(ToReadModel)];
}
