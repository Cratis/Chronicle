// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Connections;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IParticipateInConnectionLifecycle"/> for handling registrations of projections with the Kernel.
/// </summary>
public class ProjectionsRegistrar : IParticipateInConnectionLifecycle
{
    readonly IClientProjections _projectionDefinitions;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<ProjectionsRegistrar> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="projectionDefinitions">The <see cref="IClientProjections"/>.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ProjectionsRegistrar(
        IClientProjections projectionDefinitions,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<ProjectionsRegistrar> logger)
    {
        _projectionDefinitions = projectionDefinitions;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ClientConnected()
    {
        _logger.RegisterProjections();

        // var registrations = _projectionDefinitions.Definitions.Select(projection =>
        // {
        //     var pipeline = new ProjectionPipelineDefinition(
        //         projection.Identifier,
        //         new[]
        //         {
        //                 new ProjectionSinkDefinition(
        //                         "12358239-a120-4392-96d4-2b48271b904c",
        //                         projection.IsActive ? WellKnownSinkTypes.MongoDB : WellKnownSinkTypes.Null)
        //         });
        //     var serializedPipeline = JsonSerializer.SerializeToNode(pipeline, _jsonSerializerOptions)!;
        //     return new ProjectionRegistration(
        //         _projectionSerializer.Serialize(projection),
        //         serializedPipeline);
        // });

        // var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections";
        // await _connection.PerformCommand(route, new RegisterProjections(registrations));

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;
}
