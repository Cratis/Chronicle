// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Defines A1's idempotent catalog boundary; no implementation is registered here.
/// </summary>
public interface IGenerationCatalogSeam
{
    /// <summary>
    /// Adopts generation one, or reports an identical adoption or a conflict without replacing it.
    /// </summary>
    /// <param name="eventStore">The store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="record">The generation-one record.</param>
    /// <returns>The conditional adoption outcome.</returns>
    Task<AdoptResult> TryAdoptFirst(EventStoreName eventStore, EventStoreNamespaceName @namespace, MaterializationGenerationRecord record);

    /// <summary>
    /// Sets the initial epoch conditionally; repeating the identical epoch succeeds.
    /// </summary>
    /// <param name="eventStore">The store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="projection">The projection.</param>
    /// <param name="expected">The expected epoch.</param>
    /// <param name="next">The initial epoch.</param>
    /// <returns>Whether this exact epoch is established.</returns>
    Task<bool> TrySetBindingEpoch(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projection, uint expected, uint next);
}
