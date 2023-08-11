// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Models;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Schemas;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
public class Projections : IProjections
{
    static class ProjectionDefinitionCreator<TModel>
    {
        public static ProjectionDefinition CreateAndDefine(
            Type type,
            IModelNameConvention modelNameConvention,
            IEventTypes eventTypes,
            IJsonSchemaGenerator schemaGenerator,
            IServiceProvider serviceProvider,
            JsonSerializerOptions jsonSerializerOptions)
        {
            var instance = (serviceProvider.GetRequiredService(type) as IProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(instance.Identifier, modelNameConvention, eventTypes, schemaGenerator, jsonSerializerOptions);
            instance.Define(builder);
            return builder.Build();
        }
    }

    /// <inheritdoc/>
    public IImmutableList<ProjectionDefinition> Definitions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="modelNameConvention">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public Projections(
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IJsonSchemaGenerator schemaGenerator,
        IModelNameConvention modelNameConvention,
        IServiceProvider serviceProvider,
        JsonSerializerOptions jsonSerializerOptions)
    {
        Definitions = FindAllProjectionDefinitions(
            eventTypes,
            clientArtifacts,
            schemaGenerator,
            modelNameConvention,
            serviceProvider,
            jsonSerializerOptions).ToImmutableList();
    }

    IEnumerable<ProjectionDefinition> FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IJsonSchemaGenerator schemaGenerator,
        IModelNameConvention modelNameConvention,
        IServiceProvider serviceProvider,
        JsonSerializerOptions jsonSerializerOptions) =>
        clientArtifacts.Projections
                .Select(_ =>
                {
                    var modelType = _.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0]!;
                    var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(modelType);
                    var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<object>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)!;
                    return (method.Invoke(null, new object[]
                    {
                        _,
                        modelNameConvention,
                        eventTypes,
                        schemaGenerator,
                        serviceProvider,
                        jsonSerializerOptions
                    }) as ProjectionDefinition)!;
                }).ToArray();
}
