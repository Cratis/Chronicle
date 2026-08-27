// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using IReadModelsService = Cratis.Chronicle.Contracts.ReadModels.IReadModels;

namespace Cratis.Chronicle.ReadModelDefinitions;

/// <summary>
/// Represents the command for updating the definition of a read model.
/// </summary>
/// <param name="EventStore">The event store the read model belongs to.</param>
/// <param name="Identifier">The identifier of the read model.</param>
/// <param name="ContainerName">The container the read model is persisted to.</param>
/// <param name="Generation">The generation of the read model being updated.</param>
/// <param name="Schema">The schema of the read model.</param>
/// <param name="Indexes">The property paths that should be indexed.</param>
[Command]
public record UpdateReadModelDefinition(
    EventStoreName EventStore,
    ReadModelIdentifier Identifier,
    string ContainerName,
    uint Generation,
    string Schema,
    IEnumerable<string> Indexes)
{
    /// <summary>
    /// Handles the command by updating the read model definition.
    /// </summary>
    /// <param name="readModels">The <see cref="IReadModelsService"/> to update through.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(IReadModelsService readModels) =>
        readModels.UpdateDefinition(new()
        {
            EventStore = EventStore,
            ReadModel = new()
            {
                Type = new()
                {
                    Identifier = Identifier,
                    Generation = Generation
                },
                ContainerName = ContainerName,
                Schema = Schema,
                Indexes = [.. Indexes.Select(index => new Contracts.ReadModels.IndexDefinition { PropertyPath = index })]
            }
        });
}
