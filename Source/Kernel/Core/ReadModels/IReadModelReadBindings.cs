// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a system for resolving the physical destination of materialized read-model reads.
/// </summary>
public interface IReadModelReadBindings
{
    /// <summary>
    /// Resolve the sink and occurrence for a read without changing schema selection.
    /// </summary>
    /// <param name="eventStore">The event store to read.</param>
    /// <param name="eventStoreNamespace">The namespace to read.</param>
    /// <param name="definition">The read model definition.</param>
    /// <param name="occurrence">An explicit occurrence, or null for the primary container.</param>
    /// <returns>The destination for the read.</returns>
    Task<ReadModelReadBinding> Resolve(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition,
        ReadModelContainerName? occurrence = null);
}
