// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Projections.Json;
using Aksio.Cratis.Projections.Outbox;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.EventSequences.Outbox;

/// <summary>
/// Represents an implementation of <see cref="IParticipateInClientLifecycle"/> for handling registrations of outbox projections with the Kernel.
/// </summary>
public class OutboxProjectionsRegistrar : IParticipateInClientLifecycle
{
    readonly IClient _client;
    readonly IInstancesOf<IOutboxProjections> _outboxProjections;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IEnumerable<OutboxProjectionsDefinition> _outboxProjectionsDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProjectionsRegistrar"/> class.
    /// </summary>
    /// <param name="client">The Cratis <see cref="IClient"/>.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating schemas for projections.</param>
    /// <param name="outboxProjections">All instances of <see cref="IOutboxProjections"/>.</param>
    /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for serializing projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public OutboxProjectionsRegistrar(
        IClient client,
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator,
        IInstancesOf<IOutboxProjections> outboxProjections,
        IJsonProjectionSerializer projectionSerializer,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _client = client;
        _outboxProjections = outboxProjections;
        _projectionSerializer = projectionSerializer;
        _jsonSerializerOptions = jsonSerializerOptions;
        _outboxProjectionsDefinitions = _outboxProjections.Select(projections =>
        {
            var builder = new OutboxProjectionsBuilder(eventTypes, jsonSchemaGenerator, projections.Identifier, jsonSerializerOptions);
            projections.Define(builder);
            return builder.Build();
        }).ToArray();
    }

    /// <inheritdoc/>
    public async Task ClientConnected()
    {
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
