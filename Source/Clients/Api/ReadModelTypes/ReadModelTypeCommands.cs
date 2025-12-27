// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
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
    [HttpPost]
    public async Task CreateReadModel(
        [FromRoute] string eventStore,
        [FromBody] CreateReadModel command)
    {
        var identifier = Guid.NewGuid().ToString();
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            Properties =
            {
                ["id"] = new JsonSchemaProperty
                {
                    Type = JsonObjectType.String,
                    Format = "guid"
                }
            }
        };

        await _readModels.RegisterSingle(new()
        {
            EventStore = eventStore,
            Owner = Contracts.ReadModels.ReadModelOwner.Client,
            ReadModel = new()
            {
                Identifier = identifier,
                Name = command.Name,
                Generation = 1,
                Schema = schema.ToJson(),
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
    [HttpPut]
    public Task UpdateReadModelDefinition(
        [FromRoute] string eventStore,
        [FromBody] UpdateReadModelDefinition command) =>
        _readModels.UpdateDefinition(new()
        {
            EventStore = eventStore,
            ReadModel = new()
            {
                Identifier = command.Identifier,
                Name = command.Name,
                Generation = command.Generation,
                Schema = command.Schema,
                Indexes = command.Indexes.Select(i => new Contracts.ReadModels.IndexDefinition { PropertyPath = i }).ToList()
            }
        });
}
