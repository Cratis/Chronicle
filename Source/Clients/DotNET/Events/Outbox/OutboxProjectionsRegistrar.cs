// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Outbox;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using Orleans;

namespace Aksio.Cratis.Events.Outbox;

/// <summary>
/// Represents an implementation of <see cref="IOutboxProjectionsRegistrar"/>.
/// </summary>
public class OutboxProjectionsRegistrar : IOutboxProjectionsRegistrar
{
    readonly IClusterClient _clusterClient;
    readonly IExecutionContextManager _executionContextManager;
    readonly IInstancesOf<IOutboxProjections> _outboxProjections;
    readonly IEnumerable<OutboxProjectionsDefinition> _outboxProjectionsDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProjectionsRegistrar"/> class.
    /// </summary>
    /// <param name="clusterClient">Orleans <see cref="IClusterClient"/>.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating schemas for projections.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="outboxProjections">All instances of <see cref="IOutboxProjections"/>.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public OutboxProjectionsRegistrar(
        IClusterClient clusterClient,
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator,
        IExecutionContextManager executionContextManager,
        IInstancesOf<IOutboxProjections> outboxProjections,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _clusterClient = clusterClient;
        _executionContextManager = executionContextManager;
        _outboxProjections = outboxProjections;
        _outboxProjectionsDefinitions = _outboxProjections.Select(projections =>
        {
            var builder = new OutboxProjectionsBuilder(eventTypes, jsonSchemaGenerator, projections.Identifier, jsonSerializerOptions);
            projections.Define(builder);
            return builder.Build();
        }).ToArray();
    }

    /// <inheritdoc/>
    public async Task DiscoverAndRegisterAll()
    {
        _executionContextManager.Establish(ExecutionContextManager.GlobalMicroserviceId);

        var projections = _clusterClient.GetGrain<Projections.Grains.IProjections>(ExecutionContextManager.GlobalMicroserviceId);
        foreach (var outboxProjectionsDefinition in _outboxProjectionsDefinitions)
        {
            foreach (var (_, projectionDefinition) in outboxProjectionsDefinition.TargetEventTypeProjections)
            {
                var pipelineDefinition = new ProjectionPipelineDefinition(
                    projectionDefinition.Identifier,
                    new[]
                    {
                        new ProjectionSinkDefinition(
                            "06ec7e41-4424-4eb3-8dd0-defb45bc055e",
                            WellKnownProjectionSinkTypes.Outbox)
                    });
                await projections.Register(projectionDefinition, pipelineDefinition);
            }
        }
    }
}
