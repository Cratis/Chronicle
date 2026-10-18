// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines a system that recommends replaying a read model whose rows are stored under the legacy composite key format.
/// </summary>
public interface ICompositeKeyReplayRecommender
{
    /// <summary>
    /// Recommends replaying the observer that fills a read model, because its composite keys cannot be migrated in place.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read model is in.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the read model is in.</param>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> of the read model.</param>
    /// <returns>Awaitable task.</returns>
    Task Recommend(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel);
}
