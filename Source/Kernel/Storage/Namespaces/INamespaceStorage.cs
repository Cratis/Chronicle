// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Defines the storage for namespaces.
/// </summary>
public interface INamespaceStorage
{
    /// <summary>
    /// Ensures that a namespace exists.
    /// </summary>
    /// <param name="name">Name of namespace.</param>
    /// <returns>Awaitable task.</returns>
    Task Ensure(EventStoreNamespaceName name);

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

    /// <summary>
    /// Observes all namespaces.
    /// </summary>
    /// <returns>Subject with all namespaces.</returns>
    ISubject<IEnumerable<NamespaceState>> ObserveNamespaces();
}
