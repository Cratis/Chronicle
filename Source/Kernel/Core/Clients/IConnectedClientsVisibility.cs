// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Defines which connected clients a caller may see.
/// </summary>
public interface IConnectedClientsVisibility
{
    /// <summary>
    /// Gets the caller of the current HTTP request.
    /// </summary>
    /// <returns>The <see cref="ConnectionCaller"/>.</returns>
    ConnectionCaller CurrentCaller();

    /// <summary>
    /// Filters connected clients down to the ones a caller may see.
    /// </summary>
    /// <param name="clients">The <see cref="ConnectedClient"/> instances to filter.</param>
    /// <param name="caller">The <see cref="ConnectionCaller"/> asking.</param>
    /// <returns>The visible <see cref="ConnectedClient"/> instances.</returns>
    Task<IEnumerable<ConnectedClient>> Filter(IEnumerable<ConnectedClient> clients, ConnectionCaller caller);

    /// <summary>
    /// Filters connected clients down to the ones the caller of the current HTTP request may see.
    /// </summary>
    /// <param name="clients">The <see cref="ConnectedClient"/> instances to filter.</param>
    /// <returns>The visible <see cref="ConnectedClient"/> instances.</returns>
    Task<IEnumerable<ConnectedClient>> FilterForCurrentRequest(IEnumerable<ConnectedClient> clients);
}
