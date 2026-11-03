// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Defines how a backend maps event stores and namespaces to physical names, and whether they are taken.
/// </summary>
/// <remarks>
/// Only used when creating a new event store or namespace and by the startup audit, never when reading or appending.
/// Whether two physical names are the same resource is decided by the backend, using the provider's own equality
/// (for instance the server collation), never by folding the names in the caller.
/// </remarks>
public interface IResourcePhysicalNames
{
    /// <summary>
    /// Get the physical names an event store or namespace would use.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name, or null for the event store itself.</param>
    /// <returns>The physical names.</returns>
    IReadOnlyCollection<string> For(EventStoreName eventStore, EventStoreNamespaceName? @namespace);

    /// <summary>
    /// Check whether any of the physical names already exists, as the provider compares them.
    /// </summary>
    /// <param name="names">The physical names to look for.</param>
    /// <returns>True if one of them names an existing resource.</returns>
    Task<bool> AnyExists(IReadOnlyCollection<string> names);
}
