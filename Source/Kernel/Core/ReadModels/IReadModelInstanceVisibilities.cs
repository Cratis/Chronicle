// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a system for deciding which stored roots of a read model a read returns.
/// </summary>
public interface IReadModelInstanceVisibilities
{
    /// <summary>
    /// Gets the <see cref="ReadModelInstanceVisibility"/> for a read model.
    /// </summary>
    /// <param name="eventStore">The event store the read model belongs to.</param>
    /// <param name="eventStoreNamespace">The namespace being read.</param>
    /// <param name="definition">The read model definition.</param>
    /// <returns>The visibility to read with.</returns>
    Task<ReadModelInstanceVisibility> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition);
}
