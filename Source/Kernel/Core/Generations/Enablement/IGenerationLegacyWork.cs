// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Reads legacy replay evidence without changing jobs or replay contexts.
/// </summary>
public interface IGenerationLegacyWork
{
    /// <summary>
    /// Reports work that cannot safely be adopted.
    /// </summary>
    /// <param name="eventStore">The store.</param>
    /// <returns>Blocking evidence, or an empty list when reconciled.</returns>
    Task<IReadOnlyList<GenerationInitializationBlocker>> Inspect(EventStoreName eventStore);
}
