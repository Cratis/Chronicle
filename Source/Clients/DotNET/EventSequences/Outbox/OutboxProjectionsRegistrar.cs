// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Projections.Json;
using Aksio.Cratis.Projections.Outbox;
using Aksio.Models;
using Aksio.Schemas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.EventSequences.Outbox;

/// <summary>
/// Represents an implementation of <see cref="IParticipateInClientLifecycle"/> for handling registrations of outbox projections with the Kernel.
/// </summary>
public class OutboxProjectionsRegistrar : IParticipateInClientLifecycle
{
    readonly IClient _client;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<OutboxProjectionsRegistrar> _logger;
    readonly IEnumerable<OutboxProjectionsDefinition> _outboxProjectionsDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProjectionsRegistrar"/> class.
    /// </summary>
    /// <param name="client">The Cratis <see cref="IClient"/>.</param>
    /// <param name="modelNameConvention">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating schemas for projections.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for resolving instances.</param>
    /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for serializing projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public OutboxProjectionsRegistrar(
        IClient client,
        IModelNameConvention modelNameConvention,
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator,
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IJsonProjectionSerializer projectionSerializer,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<OutboxProjectionsRegistrar> logger)
    {
        _client = client;
        _projectionSerializer = projectionSerializer;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
        _outboxProjectionsDefinitions = clientArtifacts.OutboxProjections.Select(projectionsType =>
        {
            var projections = (serviceProvider.GetRequiredService(projectionsType) as IOutboxProjections)!;
            var builder = new OutboxProjectionsBuilder(modelNameConvention, eventTypes, jsonSchemaGenerator, projections.Identifier, jsonSerializerOptions);
            projections.Define(builder);
            return builder.Build();
        }).ToArray();
    }

    /// <inheritdoc/>
    public async Task ClientConnected()
    {
        _logger.RegisteringOutboxProjections();

        var registrations = _outboxProjectionsDefinitions.SelectMany(_ => _.TargetEventTypeProjections.Values).Select(projection =>
        {
            var pipeline = new ProjectionPipelineDefinition(
                projection.Identifier,
                new[]
                {
                    new ProjectionSinkDefinition(
                        "06ec7e41-4424-4eb3-8dd0-defb45bc055e",
                        WellKnownProjectionSinkTypes.Outbox)
                });
            var serializedPipeline = JsonSerializer.SerializeToNode(pipeline, _jsonSerializerOptions)!;

            return new ProjectionRegistration(
                _projectionSerializer.Serialize(projection),
                serializedPipeline);
        }).ToArray();

        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections";
        await _client.PerformCommand(route, new RegisterProjections(registrations));
    }

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;
}
