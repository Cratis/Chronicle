// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Api.ReadModels;

/// <summary>
/// Represents an occurrence of a replayed read model.
/// </summary>
/// <param name="Occurred">When the occurrence happened.</param>
/// <param name="RevertModel">Name of the revert read model.</param>
/// <param name="Generation">The generation of the read model.</param>
[ReadModel]
public record ReadModelOccurrence(DateTimeOffset Occurred, string RevertModel, ulong Generation)
{
    /// <summary>
    /// Gets all read model occurrences for a given read model.
    /// </summary>
    /// <param name="readModels"><see cref="IReadModels"/> for working with read models.</param>
    /// <param name="eventStore">The event store to get occurrences for.</param>
    /// <param name="namespace">The namespace of the read model to get occurrences for.</param>
    /// <param name="readModelName">The name of the read model to get occurrences for.</param>
    /// <returns>Collection of read model occurrences.</returns>
    internal static async Task<IEnumerable<ReadModelOccurrence>> ReadModelOccurrences(
        IReadModels readModels,
        string eventStore,
        string @namespace,
        string readModelName)
    {
        var response = await readModels.GetOccurrences(new()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            Type = new()
            {
                Identifier = readModelName
            }
        });
        return response.Occurrences.Select(o => new ReadModelOccurrence(o.Occurred, o.RevertModel, o.Type.Generation));
    }
}
