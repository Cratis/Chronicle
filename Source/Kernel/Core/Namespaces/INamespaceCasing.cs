// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Resolves namespace casing within an event store's own namespace list.
/// </summary>
public interface INamespaceCasing
{
    /// <summary>
    /// Resolves an existing namespace without creating or caching an unknown name.
    /// </summary>
    /// <param name="eventStore">The event store owning the namespace.</param>
    /// <param name="namespace">The requested namespace.</param>
    /// <returns>The stored casing, or the requested name when unknown.</returns>
    ValueTask<EventStoreNamespaceName> Canonicalize(EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
