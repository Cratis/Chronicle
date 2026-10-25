// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Reads persisted build intents from the current materialization catalog without admitting or dispatching work.
/// </summary>
public interface IGenerationBuildRecovery
{
    /// <summary>
    /// Resolves the original intent or an explicit refusal for each later Building generation of the current binding.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="projection">The projection.</param>
    /// <returns>The catalog-bounded outcomes; empty when generations are disabled, the floor is not established, or the binding is absent.</returns>
    Task<IReadOnlyCollection<RecoverableGenerationBuild>> Resolve(EventStoreName store, EventStoreNamespaceName ns, ProjectionId projection);
}
