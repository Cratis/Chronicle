// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Api.ReadModelTypes;

/// <summary>
/// Represents a read model definition.
/// </summary>
/// <param name="Identifier">Identifier of the read model.</param>
/// <param name="Generation">Generation of the read model.</param>
/// <param name="Name">Name of the read model.</param>
/// <param name="DisplayName">Display name of the read model.</param>
/// <param name="Schema">JSON schema for the read model.</param>
/// <param name="Indexes">Collection of property paths for indexes.</param>
[ReadModel]
public record ReadModelDefinition(
    string Identifier,
    ulong Generation,
    string Name,
    string DisplayName,
    string Schema,
    IEnumerable<string> Indexes)
{
    /// <summary>
    /// Get all read model definitions.
    /// </summary>
    /// <param name="readModels"><see cref="IReadModels"/> for working with read models.</param>
    /// <param name="eventStore">The event store to get read models for.</param>
    /// <returns>Collection of read model definitions.</returns>
    internal static async Task<IEnumerable<ReadModelDefinition>> AllReadModelDefinitions(IReadModels readModels, string eventStore)
    {
        var response = await readModels.GetDefinitions(new()
        {
            EventStore = eventStore
        });

        return response.ReadModels.Select(rm => new ReadModelDefinition(
            rm.Type.Identifier,
            rm.Type.Generation,
            rm.Name,
            rm.DisplayName,
            rm.Schema,
            rm.Indexes.Select(i => i.PropertyPath)));
    }
}
