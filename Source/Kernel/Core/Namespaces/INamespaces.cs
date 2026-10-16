// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Defines a system that manages namespaces.
/// </summary>
public interface INamespaces : IGrainWithStringKey
{
    /// <summary>
    /// Ensure default namespaces.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task EnsureDefault();

    /// <summary>
    /// Ensure that a namespace exists.
    /// </summary>
    /// <param name="namespace">The namespace to ensure.</param>
    /// <returns>Awaitable task.</returns>
    Task Ensure(EventStoreNamespaceName @namespace);

    /// <summary>
    /// Get all namespaces.
    /// </summary>
    /// <returns>Collection of namespaces for the event store.</returns>
    Task<IEnumerable<EventStoreNamespaceName>> GetAll();

    /// <summary>
    /// Forget a namespace that has been removed: drop it from the namespaces this grain reports and record that it
    /// was removed. Use <see cref="INamespaceRemover"/> to remove a namespace; this is its last step.
    /// </summary>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to forget.</param>
    /// <returns>Awaitable task.</returns>
    Task Forget(EventStoreNamespaceName @namespace);
}
