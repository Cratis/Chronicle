// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapterProjectionFor{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public class AdapterProjectionFor<TModel> : IAdapterProjectionFor<TModel>
{
    readonly IImmediateProjections _immediateProjections;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterProjectionFor{TModel}"/> class.
    /// </summary>
    /// <param name="projectionDefinition">The <see cref="Definition"/> to work with.</param>
    /// <param name="immediateProjections">The <see cref="IImmediateProjections"/> to perform projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    public AdapterProjectionFor(
        ProjectionDefinition projectionDefinition,
        IImmediateProjections immediateProjections,
        JsonSerializerOptions jsonSerializerOptions)
    {
        Definition = projectionDefinition;
        _immediateProjections = immediateProjections;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public ProjectionDefinition Definition { get; }

    /// <inheritdoc/>
    public async Task<AdapterProjectionResult<TModel>> GetById(ModelKey modelKey)
    {
        var result = await _immediateProjections.GetInstanceById(Definition.Identifier, modelKey);
        var model = result.Model.Deserialize<TModel>(_jsonSerializerOptions)!;
        return new(model, result.AffectedProperties, result.ProjectedEventsCount);
    }
}
