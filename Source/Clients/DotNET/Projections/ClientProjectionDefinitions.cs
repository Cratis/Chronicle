// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Integration;
using Aksio.Cratis.Models;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Rules;
using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IClientProjectionDefinitions"/>.
/// </summary>
[Singleton]
public class ClientProjectionDefinitions : IClientProjectionDefinitions
{
    static class ProjectionDefinitionCreator<TModel>
    {
        public static ProjectionDefinition CreateAndDefine(Type type, IModelNameConvention modelNameConvention, IEventTypes eventTypes, IJsonSchemaGenerator schemaGenerator, JsonSerializerOptions jsonSerializerOptions)
        {
            var instance = (Activator.CreateInstance(type) as IProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(instance.Identifier, modelNameConvention, eventTypes, schemaGenerator, jsonSerializerOptions);
            instance.Define(builder);
            return builder.Build();
        }
    }

    readonly List<ProjectionDefinition> _projections = new();
    readonly IModelNameConvention _modelNameConvention;

    /// <inheritdoc/>
    public IEnumerable<ProjectionDefinition> All => _projections;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientProjectionDefinitions"/> class.
    /// </summary>
    /// <param name="immediateProjections">All the <see cref="IImmediateProjections"/>.</param>
    /// <param name="rulesProjections"><see cref="IRulesProjections"/> for getting projection definitions related to rules.</param>
    /// <param name="adapters"><see cref="IAdapters"/> for getting adapters projection definitions.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="modelNameConvention">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public ClientProjectionDefinitions(
        IImmediateProjections immediateProjections,
        IRulesProjections rulesProjections,
        IAdapters adapters,
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IJsonSchemaGenerator schemaGenerator,
        IModelNameConvention modelNameConvention,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _modelNameConvention = modelNameConvention;
        _projections.AddRange(FindAllProjectionDefinitions(eventTypes, clientArtifacts, schemaGenerator, jsonSerializerOptions));
        _projections.AddRange(immediateProjections.Definitions);
        _projections.AddRange(adapters.Definitions);
        _projections.AddRange(rulesProjections.All);
    }

    IEnumerable<ProjectionDefinition> FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions) =>
        clientArtifacts.Projections
                .Select(_ =>
                {
                    var modelType = _.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0]!;
                    var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(modelType);
                    var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<object>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)!;
                    return (method.Invoke(null, new object[] { _, _modelNameConvention, eventTypes, schemaGenerator, jsonSerializerOptions }) as ProjectionDefinition)!;
                }).ToArray();
}
