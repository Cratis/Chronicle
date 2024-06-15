// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Schemas;
using Cratis.Models;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapterProjectionFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AdapterProjectionFactory"/> class.
/// </remarks>
/// <param name="modelNameResolver">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> to use.</param>
/// <param name="schemaGenerator">The <see cref="IJsonSchemaGenerator"/> for generating schemas.</param>
/// <param name="immediateProjections">The <see cref="IImmediateProjections"/> to perform projections.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
public class AdapterProjectionFactory(
    IModelNameResolver modelNameResolver,
    IEventTypes eventTypes,
    IJsonSchemaGenerator schemaGenerator,
    IImmediateProjections immediateProjections,
    JsonSerializerOptions jsonSerializerOptions) : IAdapterProjectionFactory
{
    /// <inheritdoc/>
    public Task<IAdapterProjectionFor<TModel>> CreateFor<TModel, TExternalModel>(IAdapterFor<TModel, TExternalModel> adapter)
    {
        var projectionBuilder = new ProjectionBuilderFor<TModel>(adapter.Identifier.Value, modelNameResolver, eventTypes, schemaGenerator, jsonSerializerOptions);
        adapter.DefineModel(projectionBuilder);
        var projectionDefinition = projectionBuilder.Build();
        projectionDefinition.IsActive = false;
        return Task.FromResult<IAdapterProjectionFor<TModel>>(new AdapterProjectionFor<TModel>(projectionDefinition, immediateProjections, jsonSerializerOptions));
    }
}
