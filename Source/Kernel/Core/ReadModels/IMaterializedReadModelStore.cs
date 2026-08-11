// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a system for reading read model instances from the materialized store.
/// </summary>
/// <remarks>
/// A read model is materialized when an observer — a projection or a reducer — writes its state to a sink.
/// The state is then already there to be read, so it does not have to be projected or reduced again, and it
/// is released of its PII before it leaves the kernel.
/// </remarks>
public interface IMaterializedReadModelStore
{
    /// <summary>
    /// Check whether a read model is materialized.
    /// </summary>
    /// <param name="definition">The <see cref="ReadModelDefinition"/> to check.</param>
    /// <returns>True if the read model is materialized, false if not.</returns>
    bool IsMaterialized(ReadModelDefinition definition);

    /// <summary>
    /// Find a single instance by its key.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read model belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the read model belongs to.</param>
    /// <param name="definition">The <see cref="ReadModelDefinition"/> to find the instance for.</param>
    /// <param name="key">The <see cref="Key"/> of the instance.</param>
    /// <returns>The released instance, or null when the store holds no instance for the key.</returns>
    Task<ExpandoObject?> FindByKey(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition,
        Key key);

    /// <summary>
    /// Get every instance of a read model.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read model belongs to.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the read model belongs to.</param>
    /// <param name="definition">The <see cref="ReadModelDefinition"/> to get instances for.</param>
    /// <returns>The released instances.</returns>
    Task<IEnumerable<ExpandoObject>> GetAllInstances(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition);
}
