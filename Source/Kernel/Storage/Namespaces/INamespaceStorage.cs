// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Defines the storage for namespaces.
/// </summary>
public interface INamespaceStorage
{
    /// <summary>
    /// Retrieves all namespace states.
    /// </summary>
    /// <returns>An asynchronous operation that returns a collection of NamespaceState objects.</returns>
    Task<IEnumerable<NamespaceState>> GetAll();

    /// <summary>
    /// Creates a new namespace state.
    /// </summary>
    /// <param name="name">The name of the namespace.</param>
    /// <param name="created">The time it was created.</param>
    /// <returns>An asynchronous operation.</returns>
    Task Create(EventStoreNamespaceName name, DateTimeOffset created);

    /// <summary>
    /// Deletes a namespace state.
    /// </summary>
    /// <param name="name">The name of the namespace to delete.</param>
    /// <returns>An asynchronous operation.</returns>
    Task Delete(EventStoreNamespaceName name);
}
