// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Preserves and checks database-local provenance before read-model operations.
/// </summary>
public interface IReadModelSinkProvenance
{
    /// <summary>
    /// Preserves discoverable evidence before definitions can change.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The preservation operation.</returns>
    Task Preserve(EventStoreName eventStore);

    /// <summary>
    /// Inventories the actual target scope and checks its current database marker.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The actual target namespace.</param>
    /// <param name="definition">The target definition.</param>
    /// <returns>The eligibility check.</returns>
    Task EnsureReadable(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition definition);
}
