// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents the collections a read model's MongoDB sink owns.
/// </summary>
/// <param name="Primary">The primary collection - the read model's own container that readers read.</param>
/// <param name="Replay">The replay shadow a replay rebuilds into.</param>
/// <param name="Promoting">The collection a rebuilt replay shadow is claimed into while it is being promoted to the primary.</param>
public record SinkCollectionNames(string Primary, string Replay, string Promoting)
{
    /// <summary>
    /// Get the collection names for a container.
    /// </summary>
    /// <param name="container">The <see cref="ReadModelContainerName"/> of the read model.</param>
    /// <returns>The <see cref="SinkCollectionNames"/>.</returns>
    public static SinkCollectionNames For(ReadModelContainerName container) =>
        new(container.Value, $"replay-{container.Value}", $"replay-{container.Value}-promoting");
}
