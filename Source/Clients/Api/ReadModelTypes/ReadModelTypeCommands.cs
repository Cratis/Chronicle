// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Sinks;
using NJsonSchema;
using IReadModelsService = Cratis.Chronicle.Contracts.ReadModels.IReadModels;

namespace Cratis.Chronicle.Api.ReadModelTypes;

/// <summary>
/// Represents the API for working with read model type commands.
/// </summary>
[Route("/api/event-store/{eventStore}/read-model-types")]
public class ReadModelTypeCommands : ControllerBase
{
    readonly IReadModelsService _readModels;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelTypeCommands"/> class.
    /// </summary>
    /// <param name="readModels"><see cref="IReadModelsService"/> for working with read models.</param>
    internal ReadModelTypeCommands(IReadModelsService readModels)
    {
        _readModels = readModels;
    }

    /// <summary>
    /// Create a new read model type.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="command">Command for creating the read model.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("create")]
    public async Task CreateReadModel(
        [FromRoute] string eventStore,
        [FromBody] CreateReadModel command)
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object
        };

        await _readModels.RegisterSingle(new()
        {
            EventStore = eventStore,
            Owner = Contracts.ReadModels.ReadModelOwner.Client,
            Source = Contracts.ReadModels.ReadModelSource.User,
            ReadModel = new()
            {
                Type = new()
                {
                    Identifier = command.Identifier,
                    Generation = 1,
                },
                ContainerName = command.ContainerName,
                DisplayName = command.DisplayName,
                Sink = new()
                {
                    TypeId = WellKnownSinkTypes.MongoDB.Value,
                    ConfigurationId = Guid.Empty
                },
                Schema = command.Schema ?? schema.ToJson(),
                Indexes = []
            }
        });
    }

    /// <summary>
    /// Update a read model definition.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="command">Command for updating the read model definition.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("update")]
    public Task UpdateReadModelDefinition(
        [FromRoute] string eventStore,
        [FromBody] UpdateReadModelDefinition command) =>
        _readModels.UpdateDefinition(new()
        {
            EventStore = eventStore,
            ReadModel = new()
            {
                Type = new()
                {
                    Identifier = command.Identifier,
                    Generation = command.Generation
                },
                ContainerName = command.ContainerName,
                Schema = command.Schema,
                Indexes = command.Indexes.Select(i => new Contracts.ReadModels.IndexDefinition { PropertyPath = i }).ToList()
            }
        });
}
