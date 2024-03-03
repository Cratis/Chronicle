// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Cratis.Events;
using Cratis.Kernel.Contracts.Projections;
using Cratis.Models;
using Cratis.Schemas;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
public class Projections : IProjections
{
    readonly IEventStore _eventStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the projections belongs to.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="modelNameResolver">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public Projections(
        IEventStore eventStore,
        IClientArtifactsProvider clientArtifacts,
        IJsonSchemaGenerator schemaGenerator,
        IModelNameResolver modelNameResolver,
        IServiceProvider serviceProvider,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _eventStore = eventStore;
        Definitions = FindAllProjectionDefinitions(
            _eventStore.EventTypes,
            clientArtifacts,
            schemaGenerator,
            modelNameResolver,
            serviceProvider,
            jsonSerializerOptions).ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<ProjectionDefinition> Definitions { get; }

    /// <inheritdoc/>
    public Task Discover()
    {
        return Task.CompletedTask;
    }

    IEnumerable<ProjectionDefinition> FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IJsonSchemaGenerator schemaGenerator,
        IModelNameResolver modelNameResolver,
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
                        modelNameResolver,
                        eventTypes,
                        schemaGenerator,
                        serviceProvider,
                        jsonSerializerOptions
                    }) as ProjectionDefinition)!;
                }).ToArray();

    static class ProjectionDefinitionCreator<TModel>
    {
        public static ProjectionDefinition CreateAndDefine(
            Type type,
            IModelNameResolver modelNameResolver,
            IEventTypes eventTypes,
            IJsonSchemaGenerator schemaGenerator,
            IServiceProvider serviceProvider,
            JsonSerializerOptions jsonSerializerOptions)
        {
            var instance = (serviceProvider.GetRequiredService(type) as IProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(instance.Identifier, modelNameResolver, eventTypes, schemaGenerator, jsonSerializerOptions);
            instance.Define(builder);
            return builder.Build();
        }
    }
}
