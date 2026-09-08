// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using IReadModelsService = Cratis.Chronicle.Contracts.ReadModels.IReadModels;

namespace Cratis.Chronicle.ReadModelDefinitions;

/// <summary>
/// Represents the command for creating a user-defined read model.
/// </summary>
/// <param name="EventStore">The event store the read model belongs to.</param>
/// <param name="Identifier">The identifier of the read model.</param>
/// <param name="DisplayName">The name shown for the read model.</param>
/// <param name="ContainerName">The container the read model is persisted to.</param>
/// <param name="Schema">Optional schema. An empty object schema is used when not supplied.</param>
[Command]
public record CreateReadModel(
    EventStoreName EventStore,
    ReadModelIdentifier Identifier,
    string DisplayName,
    string ContainerName,
    string? Schema = null)
{
    /// <summary>
    /// Handles the command by registering the read model definition.
    /// </summary>
    /// <param name="readModels">The <see cref="IReadModelsService"/> to register through.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IReadModelsService readModels)
    {
        var emptySchema = new JsonSchema { Type = JsonObjectType.Object };

        return readModels.RegisterSingle(new()
        {
            EventStore = EventStore,
            Owner = Contracts.ReadModels.ReadModelOwner.Client,
            Source = Contracts.ReadModels.ReadModelSource.User,
            ReadModel = new()
            {
                Type = new()
                {
                    Identifier = Identifier,
                    Generation = 1
                },
                ContainerName = ContainerName,
                DisplayName = DisplayName,
                Sink = new()
                {
                    TypeId = WellKnownSinkTypes.MongoDB.Value,
                    ConfigurationId = Guid.Empty
                },
                Schema = Schema ?? emptySchema.ToJson(),
                Indexes = []
            }
        });
    }
}
