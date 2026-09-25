// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Reads decision-consistent event-source-keyed projection instances from the event log.
/// </summary>
public interface IDecisionReadModels
{
    /// <summary>
    /// Reads an instance or its exact absence, together with the last matching event and projected event types.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="key">The event source key.</param>
    /// <returns>The instance and its concurrency scope inputs.</returns>
    Task<ReadModelInstance<TReadModel>> GetInstanceForDecision<TReadModel>(ReadModelKey key);
}
