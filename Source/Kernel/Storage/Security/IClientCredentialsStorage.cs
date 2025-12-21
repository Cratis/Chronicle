// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Defines the storage for Chronicle client credentials.
/// </summary>
public interface IClientCredentialsStorage
{
    /// <summary>
    /// Gets an <see cref="IObservable{T}"/> for all instances of <see cref="ChronicleClient"/>.
    /// </summary>
    /// <returns>An observable of collection of <see cref="ChronicleClient"/>.</returns>
    ISubject<IEnumerable<ChronicleClient>> ObserveAll();

    /// <summary>
    /// Gets a client by their unique identifier.
    /// </summary>
    /// <param name="id">The client ID.</param>
    /// <returns>The client if found, null otherwise.</returns>
    Task<ChronicleClient?> GetById(string id);

    /// <summary>
    /// Gets a client by their client identifier.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>The client if found, null otherwise.</returns>
    Task<ChronicleClient?> GetByClientId(string clientId);

    /// <summary>
    /// Gets all client credentials.
    /// </summary>
    /// <returns>All client credentials in the system.</returns>
    Task<IEnumerable<ChronicleClient>> GetAll();

    /// <summary>
    /// Creates a new client credentials.
    /// </summary>
    /// <param name="client">The client to create.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Create(ChronicleClient client);

    /// <summary>
    /// Updates an existing client credentials.
    /// </summary>
    /// <param name="client">The client to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Update(ChronicleClient client);

    /// <summary>
    /// Deletes a client credentials.
    /// </summary>
    /// <param name="id">The ID of the client to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Delete(string id);
}
