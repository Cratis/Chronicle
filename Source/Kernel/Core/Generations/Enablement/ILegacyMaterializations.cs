// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Reads the materializations already serving the store.
/// </summary>
public interface ILegacyMaterializations
{
    /// <summary>
    /// Reads adoption candidates without rebuilding data.
    /// </summary>
    /// <param name="eventStore">The store.</param>
    /// <returns>The existing materializations.</returns>
    Task<IReadOnlyList<LegacyMaterialization>> GetAll(EventStoreName eventStore);
}
