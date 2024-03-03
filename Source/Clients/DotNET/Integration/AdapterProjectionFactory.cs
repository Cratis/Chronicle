// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Events;
using Cratis.Models;
using Cratis.Projections;
using Cratis.Schemas;

namespace Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapterProjectionFactory"/>.
/// </summary>
public class AdapterProjectionFactory : IAdapterProjectionFactory
{
    readonly IModelNameResolver _modelNameResolver;
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _schemaGenerator;
    readonly IImmediateProjections _immediateProjections;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterProjectionFactory"/> class.
    /// </summary>
    /// <param name="modelNameResolver">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> to use.</param>
    /// <param name="schemaGenerator">The <see cref="IJsonSchemaGenerator"/> for generating schemas.</param>
    /// <param name="immediateProjections">The <see cref="IImmediateProjections"/> to perform projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    public AdapterProjectionFactory(
        IModelNameResolver modelNameResolver,
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        IImmediateProjections immediateProjections,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _modelNameResolver = modelNameResolver;
        _eventTypes = eventTypes;
        _schemaGenerator = schemaGenerator;
        _immediateProjections = immediateProjections;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public Task<IAdapterProjectionFor<TModel>> CreateFor<TModel, TExternalModel>(IAdapterFor<TModel, TExternalModel> adapter)
    {
        var projectionBuilder = new ProjectionBuilderFor<TModel>(adapter.Identifier.Value, _modelNameResolver, _eventTypes, _schemaGenerator, _jsonSerializerOptions);
        adapter.DefineModel(projectionBuilder);
        var projectionDefinition = projectionBuilder.Build();
        projectionDefinition.IsActive = false;
        return Task.FromResult<IAdapterProjectionFor<TModel>>(new AdapterProjectionFor<TModel>(projectionDefinition, _immediateProjections, _jsonSerializerOptions));
    }
}
