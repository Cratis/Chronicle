// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsRegistrar"/>.
/// </summary>
public class ProjectionsRegistrar : IProjectionsRegistrar
{
    static class ProjectionDefinitionCreator<TModel>
    {
        public static ProjectionDefinition CreateAndDefine(Type type, IEventTypes eventTypes, IJsonSchemaGenerator schemaGenerator, JsonSerializerOptions jsonSerializerOptions)
        {
            var instance = (Activator.CreateInstance(type) as IProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(instance.Identifier, eventTypes, schemaGenerator, jsonSerializerOptions);
            instance.Define(builder);
            return builder.Build();
        }
    }

    readonly IEnumerable<ProjectionDefinition> _projections;
    readonly IClient _client;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="client">The Cratis <see cref="IClient"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public ProjectionsRegistrar(
        IClient client,
        IExecutionContextManager executionContextManager,
        IEventTypes eventTypes,
        ITypes types,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _projections = FindAllProjectionDefinitions(eventTypes, types, schemaGenerator, jsonSerializerOptions);
        _client = client;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Find all projection definitions.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
    /// <param name="types"><see cref="ITypes"/> to find from.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating the schema for the model.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    /// <returns>Collection of <see cref="ProjectionDefinition"/>.</returns>
    public static IEnumerable<ProjectionDefinition> FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        ITypes types,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions) =>
        types.All
                .Where(_ => _.HasInterface(typeof(IProjectionFor<>)))
                .Select(_ =>
                {
                    var modelType = _.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0]!;
                    var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(modelType);
                    var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<object>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)!;
                    return (method.Invoke(null, new object[] { _, eventTypes, schemaGenerator, jsonSerializerOptions }) as ProjectionDefinition)!;
                }).ToArray();

    /// <inheritdoc/>
    public async Task DiscoverAndRegisterAll()
    {
        var registrations = _projections.Select(projection => new ProjectionRegistration(projection, new(
            projection.Identifier,
            new[]
            {
                new ProjectionSinkDefinition(
                    "12358239-a120-4392-96d4-2b48271b904c",
                    WellKnownProjectionSinkTypes.MongoDB)
            })));

        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections";
        await _client.PerformCommand(route, new RegisterProjections(registrations));
    }
}
