// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapterProjectionFor{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public class AdapterProjectionFor<TModel> : IAdapterProjectionFor<TModel>
{
    readonly ProjectionDefinition _projectionDefinition;
    readonly IImmediateProjections _immediateProjections;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterProjectionFor{TModel}"/> class.
    /// </summary>
    /// <param name="projectionDefinition">The <see cref="ProjectionDefinition"/> to work with.</param>
    /// <param name="immediateProjections">The <see cref="IImmediateProjections"/> to perform projections.</param>
    public AdapterProjectionFor(
        ProjectionDefinition projectionDefinition,
        IImmediateProjections immediateProjections)
    {
        _projectionDefinition = projectionDefinition;
        _immediateProjections = immediateProjections;
    }

    /// <inheritdoc/>
    public async Task<AdapterProjectionResult<TModel>> GetById(ModelKey modelKey)
    {
        var result = await _immediateProjections.GetInstanceById<TModel>(modelKey, _projectionDefinition);
        return new(result.Model, result.AffectedProperties, result.ProjectedEventsCount);
    }
}
