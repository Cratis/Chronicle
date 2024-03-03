// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Kernel.Contracts.Projections;
using Cratis.Models;
using Cratis.Projections;

namespace Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapterProjectionFor{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="AdapterProjectionFor{TModel}"/> class.
/// </remarks>
/// <param name="projectionDefinition">The <see cref="Definition"/> to work with.</param>
/// <param name="immediateProjections">The <see cref="IImmediateProjections"/> to perform projections.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
public class AdapterProjectionFor<TModel>(
    ProjectionDefinition projectionDefinition,
    IImmediateProjections immediateProjections,
    JsonSerializerOptions jsonSerializerOptions) : IAdapterProjectionFor<TModel>
{
    /// <inheritdoc/>
    public ProjectionDefinition Definition { get; } = projectionDefinition;

    /// <inheritdoc/>
    public async Task<AdapterProjectionResult<TModel>> GetById(ModelKey modelKey)
    {
        var result = await immediateProjections.GetInstanceById(Definition.Identifier, modelKey);
        var model = result.Model.Deserialize<TModel>(jsonSerializerOptions)!;
        return new(model, result.AffectedProperties, result.ProjectedEventsCount);
    }
}
