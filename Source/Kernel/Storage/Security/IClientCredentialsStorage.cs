// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Defines a storage interface for client credentials.
/// </summary>
public interface IClientCredentialsStorage
{
    /// <summary>
    /// Gets a client by its identifier.
    /// </summary>
    /// <param name="id">The client's unique identifier.</param>
    /// <returns>The client if found, null otherwise.</returns>
    Task<ChronicleClient?> GetById(string id);

    /// <summary>
    /// Gets a client by its client ID.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <returns>The client if found, null otherwise.</returns>
    Task<ChronicleClient?> GetByClientId(string clientId);

    /// <summary>
    /// Creates a new client.
    /// </summary>
    /// <param name="client">The client to create.</param>
    /// <returns>Awaitable task.</returns>
    Task Create(ChronicleClient client);

    /// <summary>
    /// Updates an existing client.
    /// </summary>
    /// <param name="client">The client to update.</param>
    /// <returns>Awaitable task.</returns>
    Task Update(ChronicleClient client);

    /// <summary>
    /// Deletes a client.
    /// </summary>
    /// <param name="id">The client's unique identifier.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(string id);

    /// <summary>
    /// Gets all clients.
    /// </summary>
    /// <returns>All clients.</returns>
    Task<IEnumerable<ChronicleClient>> GetAll();
}
